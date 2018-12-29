using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private IConfiguration _config;
        private FhirImportServiceConfiguration _options;
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _cloudBlobClient;
        private CloudBlobContainer _importCloudBlobContainer;
        private CloudBlobContainer _rejectCloudBlobContainer;
        private CancellationToken _cancellationToken;

        public FhirImportService(ILogger<FhirImportService> logger,
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
                    CloudBlobClient _cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                    _importCloudBlobContainer = _cloudBlobClient.GetContainerReference(_options.ImportContainerName);
                    _rejectCloudBlobContainer = _cloudBlobClient.GetContainerReference(_options.RejectedContainerName);
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

            _timer = new Timer(ProcessInput, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds));
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
                    _logger.LogInformation($"Found item {item.ToString()}");
                }

            } while (!_cancellationToken.IsCancellationRequested && blobContinuationToken != null); // Loop while the continuation token is not null.

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
    }
}
