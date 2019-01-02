using System;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportServiceConfiguration
    {
        public string StorageConnectionString { get; set; }
        public string Authority { get; set; }
        public string Audience { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string FhirServerUrl { get; set; }
        public string ImportContainerName { get; set; } = "fhirimport";
        public string RejectedContainerName { get; set; } = "fhirrejected";
        public int PollingIntervalSeconds { get; set; } = 15;
    }
}