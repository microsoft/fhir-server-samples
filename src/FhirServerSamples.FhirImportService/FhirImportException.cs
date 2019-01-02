using System;

namespace FhirServerSamples.FhirImportService
{
    public class FhirImportException : Exception
    {
        public FhirImportException(string message)
            : base(message)
        {
        }
    }
}
