// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace Microsoft.Health
{
    public static class FhirBundleBlobTrigger
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("FhirBundleBlobTrigger")]
        public static async Task Run([BlobTrigger("fhirimport/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, ILogger log)
        {
            var newConfig = TelemetryConfiguration.CreateDefault();
            var telemetryClient = new TelemetryClient(newConfig);

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var streamReader = new StreamReader(myBlob);
            var fhirString = streamReader.ReadToEndAsync();
            
            JObject bundle;
            JArray entries;
            try
            {
                bundle = JObject.Parse(await fhirString);
            }
            catch (JsonReaderException)
            {
                log.LogError("Input file is not a valid JSON document");
                await MoveBlobToRejected(name, log);
                return;
            }

            log.LogInformation("File read");

            try
            {
                FhirImportReferenceConverter.ConvertUUIDs(bundle);
            }
            catch
            {
                log.LogError("Failed to resolve references in doc");
                await MoveBlobToRejected(name, log);
                return;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int entriesCount = 0;
            bool handled = default;
            try
            {
                entries = (JArray)bundle["entry"];
                if (entries == null)
                {
                    log.LogError("No entries found in bundle");
                    throw new FhirImportException("No entries found in bundle");
                }


                AuthenticationContext authContext;
                ClientCredential clientCredential;
                AuthenticationResult authResult;

                string bearerToken;
                entriesCount = entries.Count;

                string authority = System.Environment.GetEnvironmentVariable("Authority");
                string audience = System.Environment.GetEnvironmentVariable("Audience");
                string clientId = System.Environment.GetEnvironmentVariable("ClientId");
                string clientSecret = System.Environment.GetEnvironmentVariable("ClientSecret");
                Uri fhirServerUrl = new Uri(System.Environment.GetEnvironmentVariable("FhirServerUrl"));
                httpClient.BaseAddress = fhirServerUrl;

                int maxDegreeOfParallelism;
                if (!int.TryParse(System.Environment.GetEnvironmentVariable("MaxDegreeOfParallelism"), out maxDegreeOfParallelism))
                {
                    maxDegreeOfParallelism = 16;
                }

                try
                {
                    if(authority.Contains("connect/token"))
                    {
                        var formEntries = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("client_id", clientId),
                            new KeyValuePair<string, string>("client_secret", clientSecret),
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("scope", audience),
                            new KeyValuePair<string, string>("resource", audience),
                        };
                        var formContent = new FormUrlEncodedContent(formEntries);

                        var httpClient = new HttpClient();
                        HttpResponseMessage tokenResponse = await httpClient.PostAsync(authority, formContent);

                        var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());
                        bearerToken = tokenJson["access_token"].Value<string>();

                    }
                    else
                    {
                        authContext = new AuthenticationContext(authority);
                        clientCredential = new ClientCredential(clientId, clientSecret);
                        authResult = await authContext.AcquireTokenAsync(audience, clientCredential);
                        bearerToken = authResult.AccessToken;
                    }
                }
                catch (Exception ee)
                {
                    log.LogCritical(string.Format("Unable to obtain token to access FHIR server in FhirImportService {0}", ee.ToString()));
                    throw;
                }

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                var randomGenerator = new Random();

                //var entriesNum = Enumerable.Range(0,entries.Count-1);
                var actionBlock = new ActionBlock<int>(async i =>
                    {
                        var entry_json = ((JObject)entries[i])["resource"].ToString();
                        string resource_type = (string)((JObject)entries[i])["resource"]["resourceType"];
                        string id = (string)((JObject)entries[i])["resource"]["id"];

                        await Task.Delay(TimeSpan.FromMilliseconds(randomGenerator.Next(50)));

                        if (string.IsNullOrEmpty(entry_json))
                        {
                            log.LogError("No 'resource' section found in JSON document");
                            throw new FhirImportException("'resource' not found or empty");
                        }

                        if (string.IsNullOrEmpty(resource_type))
                        {
                            log.LogError("No resource_type found.");
                            throw new FhirImportException("No resource_type in resource.");
                        }

                        StringContent content = new StringContent(entry_json, Encoding.UTF8, "application/json");
                        var pollyDelays =
                                new[]
                                {
                                    TimeSpan.FromMilliseconds(2000 + randomGenerator.Next(50)),
                                    TimeSpan.FromMilliseconds(3000 + randomGenerator.Next(50)),
                                    TimeSpan.FromMilliseconds(5000 + randomGenerator.Next(50)),
                                    TimeSpan.FromMilliseconds(8000 + randomGenerator.Next(50))
                                };

                        var entryStopWatch = new Stopwatch();
                        HttpResponseMessage uploadResult = await Policy
                            .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                            .WaitAndRetryAsync(pollyDelays, (result, timeSpan, retryCount, context) =>
                            {
                                entryStopWatch.Stop();
                                log.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");

                                telemetryClient.TrackEvent("Failed resource request",
                                    new Dictionary<string,string>{
                                        {"statusCode", result.Result.StatusCode.ToString()},
                                        {"resourceId", $"/{resource_type}/{id}"},
                                    },
                                    new Dictionary<string, double>{
                                        {"totalSeconds", entryStopWatch.Elapsed.TotalSeconds},
                                        {"totalMilliseconds", entryStopWatch.Elapsed.TotalMilliseconds},
                                    }
                                );
                            })
                            .ExecuteAsync(async () => {
                                entryStopWatch.Start();                    
                                var message = string.IsNullOrEmpty(id)
                                    ? new HttpRequestMessage(HttpMethod.Post, new Uri($"/{resource_type}"))
                                    : new HttpRequestMessage(HttpMethod.Put, new Uri($"/{resource_type}/{id}"));

                                message.Content = content;
                                return await httpClient.SendAsync(message);
                            });

                        if (!uploadResult.IsSuccessStatusCode)
                        {
                            Task<string> resultContent = uploadResult.Content.ReadAsStringAsync();
                            log.LogError(await resultContent);

                            // Throwing a generic exception here. This will leave the blob in storage and retry.
                            throw new Exception($"Unable to upload to server. Error code {uploadResult.StatusCode}");
                        }
                        else
                        {
                            entryStopWatch.Stop();
                                telemetryClient.TrackEvent("Successful resource request",
                                    new Dictionary<string,string>{
                                        {"statusCode", uploadResult.StatusCode.ToString()},
                                        {"resourceId", $"/{resource_type}/{id}"},
                                    },
                                    new Dictionary<string, double>{
                                        {"totalSeconds", entryStopWatch.Elapsed.TotalSeconds},
                                        {"totalMilliseconds", entryStopWatch.Elapsed.TotalMilliseconds},
                                    }
                                );
                            log.LogInformation($"Uploaded /{resource_type}/{id}");
                        }
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism
                    }
                );

                for (var i = 0; i < entries.Count; i++)
                {
                    actionBlock.Post(i);
                }
                actionBlock.Complete();
                actionBlock.Completion.Wait();

                stopWatch.Stop();
                telemetryClient.TrackEvent("Upload bundle success",
                    new Dictionary<string,string>{
                        {"name", name},
                    },
                    new Dictionary<string, double>{
                        {"totalSeconds", stopWatch.Elapsed.TotalSeconds},
                        {"totalMilliseconds", stopWatch.Elapsed.TotalMilliseconds},
                        {"entryCount", entriesCount},
                    }
                );
                handled = true;

                // We are done with this blob, upload was successful, it will be delete
                await GetBlobReference("fhirimport", name, log).DeleteIfExistsAsync();
            }
            catch (FhirImportException)
            {
                if(!handled){
                    stopWatch.Stop();
                    
                    telemetryClient.TrackEvent("Upload bundle failure",
                        new Dictionary<string,string>{
                            {"name", name},
                        },
                        new Dictionary<string, double>{
                            {"totalSeconds", stopWatch.Elapsed.TotalSeconds},
                            {"totalMilliseconds", stopWatch.Elapsed.TotalMilliseconds},
                            {"entryCount", entriesCount},
                        }
                    );
                }

                await MoveBlobToRejected(name, log);
            }
        }

        private static CloudBlockBlob GetBlobReference(string containerName, string blobName, ILogger log)
        {
            var connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    var container = cloudBlobClient.GetContainerReference(containerName);
                    var blockBlob = container.GetBlockBlobReference(blobName);
                    return blockBlob;
                }
                catch
                {
                    log.LogCritical("Unable to get blob reference");
                    return null;
                }
            }
            else
            {
                log.LogCritical("Unable to parse connection string and create storage account reference");
                return null;
            }

        }

        private static async Task MoveBlobToRejected(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference("fhirimport", name, log);
            CloudBlockBlob destBlob = GetBlobReference("fhirrejected", name, log);

            await destBlob.StartCopyAsync(srcBlob);
            await srcBlob.DeleteAsync();
        }
    }
}
