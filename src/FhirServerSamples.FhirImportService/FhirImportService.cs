// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private IConfiguration _config;
        private FhirImportServiceConfiguration _options;
        private CloudStorageAccount _storageAccount;
        private CloudBlobContainer _importCloudBlobContainer;
        private CloudBlobContainer _rejectCloudBlobContainer;
        private CancellationToken _cancellationToken;
        private AuthenticationContext _authContext;
        private ClientCredential _clientCredential;
        private HttpClient _httpClient;

        public FhirImportService(
            ILogger<FhirImportService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;

            // Set up configuration
            // TODO: Check for KeyVault endpoint and retrive parameters from there
            _options = new FhirImportServiceConfiguration();
            _config.Bind("FhirImportService", _options);

            if (CloudStorageAccount.TryParse(_options.StorageConnectionString, out _storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                    _importCloudBlobContainer = cloudBlobClient.GetContainerReference(_options.ImportContainerName);
                    _rejectCloudBlobContainer = cloudBlobClient.GetContainerReference(_options.RejectedContainerName);
                }
                catch (StorageException)
                {
                    _logger.LogCritical("Error establishing storage container connections");
                    throw;
                }
            }
            else
            {
                _logger.LogCritical($"Invalid storage connection string: {_options.StorageConnectionString}");
                throw new StorageException("Invalid connection string");
            }

            // We will attempt to secure a token during setup
            try
            {
                _authContext = new AuthenticationContext(_options.Authority);
                _clientCredential = new ClientCredential(_options.ClientId, _options.ClientSecret);
                _httpClient = new HttpClient();
                _httpClient.BaseAddress = new Uri(_options.FhirServerUrl);
                var authResult = _authContext.AcquireTokenAsync(_options.Audience, _clientCredential).Result;
            }
            catch (Exception ee)
            {
                _logger.LogCritical(string.Format("Unable to obtain token to access FHIR server in FhirImportService {0}", ee.ToString()));
                throw;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            try
            {
                await _importCloudBlobContainer.CreateIfNotExistsAsync();
                await _rejectCloudBlobContainer.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                _logger.LogCritical($"Error creating blob containers with names {_options.ImportContainerName} and {_options.RejectedContainerName}");
                throw;
            }

            _timer = new Timer(ProcessInput, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.PollingIntervalSeconds));
        }

        private async void ProcessInput(object state)
        {
            // Stop the timer while processing
            _timer.Change(Timeout.Infinite, 0);

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await _importCloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);

                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    var blob = item as CloudBlockBlob;
                    if (blob != null)
                    {
                        var fhirString = await blob.DownloadTextAsync();

                        JObject bundle;
                        JArray entries;
                        try
                        {
                            bundle = JObject.Parse(fhirString);
                            bundle = (JObject)FhirImportReferenceConverter.ConvertUUIDs(bundle);
                            entries = (JArray)bundle["entry"];
                        }
                        catch (JsonReaderException)
                        {
                            _logger.LogError("Input file is not a valid JSON document");
                            await MoveBlobToRejected(blob);
                            continue; // Process the next blob
                        }

                        try
                        {
                            for (int i = 0; i < entries.Count; i++)
                            {
                                var entry_json = ((JObject)entries[i])["resource"].ToString();
                                string resource_type = (string)((JObject)entries[i])["resource"]["resourceType"];
                                string id = (string)((JObject)entries[i])["resource"]["id"];

                                if (string.IsNullOrEmpty(entry_json))
                                {
                                    _logger.LogError("No 'resource' section found in JSON document");
                                    throw new FhirImportException("'resource' not found or empty");
                                }

                                if (string.IsNullOrEmpty(resource_type))
                                {
                                    _logger.LogError("No resource_type found.");
                                    throw new FhirImportException("No resource_type in resource.");
                                }

                                // If we already have a token, we should get the cached one, otherwise, refresh
                                var authResult = _authContext.AcquireTokenAsync(_options.Audience, _clientCredential).Result;
                                _httpClient.DefaultRequestHeaders.Clear();
                                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + authResult.AccessToken);
                                StringContent content = new StringContent(entry_json, Encoding.UTF8, "application/json");

                                HttpResponseMessage uploadResult;
                                if (string.IsNullOrEmpty(id))
                                {
                                    uploadResult = await _httpClient.PostAsync($"/{resource_type}", content);
                                }
                                else
                                {
                                    uploadResult = await _httpClient.PutAsync($"/{resource_type}/{id}", content);
                                }

                                if (!uploadResult.IsSuccessStatusCode)
                                {
                                    string resultContent = await uploadResult.Content.ReadAsStringAsync();
                                    _logger.LogError(resultContent);
                                    throw new FhirImportException("Unable to upload resources to FHIR server");
                                }
                            }

                            // We are done with this blob, upload was successful, it will be delete
                            await blob.DeleteAsync();
                        }
                        catch (FhirImportException)
                        {
                            await MoveBlobToRejected(blob);
                        }
                    }
                }
            }
            while (!_cancellationToken.IsCancellationRequested && blobContinuationToken != null); // Loop while the continuation token is not null.

            // Start timer again
            _timer.Change(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), TimeSpan.FromSeconds(_options.PollingIntervalSeconds));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task MoveBlobToRejected(CloudBlockBlob blob)
        {
            CloudBlockBlob destBlob;

            // Copy source blob to destination container
            string name = blob.Uri.Segments.Last();
            destBlob = _rejectCloudBlobContainer.GetBlockBlobReference(name);
            await destBlob.StartCopyAsync(blob);

            // Remove source blob after copy is done.
            await blob.DeleteAsync();
        }
    }
}
