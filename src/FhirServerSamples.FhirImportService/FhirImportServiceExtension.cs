using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FhirServerSamples.FhirImportService
{
    public static class FhirImportServiceExtension
    {
        
        /// <summary>
        /// Adds FHIR Import Service
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="configuration">The configuration object.</param>
        public static IServiceCollection AddFhirImportService(
            this IServiceCollection services)
        {
            services.AddHostedService<FhirImportService>();
            return services;
        }
    }
}
