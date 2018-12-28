using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private IConfiguration _config;

        public FhirImportService(ILogger<FhirImportService> logger,
                                 IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("Timed Background Service is working.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
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
