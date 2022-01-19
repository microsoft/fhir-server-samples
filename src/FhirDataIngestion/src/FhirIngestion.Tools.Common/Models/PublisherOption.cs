namespace FhirIngestion.Tools.Common.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Publisher options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PublisherOption
    {
        /// <summary>
        /// Environement variable name for the AAD Client ID.
        /// </summary>
        public const string AadClientIdEnvironementVariableName = "PUBLISHER_CLIENT_ID";

        /// <summary>
        /// Environement variable name for the AAD Client Secret.
        /// </summary>
        public const string AadClientSecretEnvironementVariableName = "PUBLISHER_CLIENT_SECRET";

        /// <summary>
        /// Environement variable name for the AAD Client Secret.
        /// </summary>
        public const string AadTenantIdEnvironementVariableName = "PUBLISHER_TENANT_ID";

        private string _aadClientId;
        private string _aadClientSecret;
        private string _aadTenantId;

        /// <summary>Gets or sets a value indicating whether gets or sets the AAD Client ID.</summary>
        public string AadClientId
        {
            get
            {
                if (string.IsNullOrEmpty(_aadClientId))
                {
                    var clientId = Environment.GetEnvironmentVariable(AadClientIdEnvironementVariableName);
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        _aadClientId = clientId;
                    }
                }

                return _aadClientId;
            }

            set
            {
                _aadClientId = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether gets or sets the AAD Client Secret.</summary>
        public string AadClientSecret
        {
            get
            {
                if (string.IsNullOrEmpty(_aadClientSecret))
                {
                    var clientSecret = Environment.GetEnvironmentVariable(AadClientSecretEnvironementVariableName);
                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        _aadClientSecret = clientSecret;
                    }
                }

                return _aadClientSecret;
            }

            set
            {
                _aadClientSecret = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether gets or sets the AA resource.</summary>
        public string AadResource { get; set; }

        /// <summary>Gets or sets a value indicating whether gets or sets the AAD tenant ID.</summary>
        public string AadTenantId
        {
            get
            {
                if (string.IsNullOrEmpty(_aadTenantId))
                {
                    var tenantId = Environment.GetEnvironmentVariable(AadTenantIdEnvironementVariableName);
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        _aadTenantId = tenantId;
                    }
                }

                return _aadTenantId;
            }

            set
            {
                _aadTenantId = value;
            }
        }

        /// <summary>Gets or sets a value indicating the FHIR server API URI.</summary>
        public string FhirServerApiUri { get; set; }

        /// <summary>Gets or sets a value indicating the maximum degree of parallelism.</summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>Gets or sets a value indicating the maximum number of retry.</summary>
        public int MaxRetryCount { get; set; }

        /// <summary>Gets or sets a value indicating the metrics refresh interval.</summary>
        public int MetricsRefreshInterval { get; set; }

        /// <summary>Gets or sets a value indicating the Median First Retry Delay in seconds.</summary>
        public int MedianFirstRetryDelay { get; set; }

        /// <summary>Gets or sets a value indicating to the output response bundles directory.</summary>
        public string OutputResponseBundlesDir { get; set; }
    }
}
