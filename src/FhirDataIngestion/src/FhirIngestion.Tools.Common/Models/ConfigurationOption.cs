namespace FhirIngestion.Tools.Common.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Configuration file.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConfigurationOption
    {
        /// <summary>
        /// Environement variable name for the AAD Client ID.
        /// </summary>
        public const string ApplicationInsightKeyEnvironmentVariable = "APP_INSIGHT_KEY";

        private string _appKey;

        /// <summary>Gets or sets a value indicating whether gets or sets the verbose logs.</summary>
        public bool VerboseLogs { get; set; }

        /// <summary>Gets or sets a value indicating whether gets or sets the input directory.</summary>
        public string InputDir { get; set; }

        /// <summary>Gets or sets a value indicating whether gets or sets the output directory.</summary>
        public string OutputDir { get; set; }

        /// <summary>Gets or sets a value indicating whether gets or sets the Application Insights instrumentation key.</summary>
        public string ApplicationInsightsInstrumentationKey
        {
            get
            {
                if (string.IsNullOrEmpty(_appKey))
                {
                    var appKey = Environment.GetEnvironmentVariable(ApplicationInsightKeyEnvironmentVariable);
                    if (!string.IsNullOrEmpty(appKey))
                    {
                        _appKey = appKey;
                    }
                }

                return _appKey;
            }

            set
            {
                _appKey = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether gets or sets the stages.</summary>
        public StagesOptions Stages { get; set; }

        /// <summary>
        /// Checks if the configuration is valid.
        /// </summary>
        /// <returns>True if the confirugation is valid.</returns>
        public bool IsValid()
        {

            if (string.IsNullOrEmpty(InputDir))
            {
                MessageHelper.Error($"ERROR: Input folder is mandatory.");
                return false;
            }

            if (!Directory.Exists(InputDir))
            {
                MessageHelper.Error($"ERROR: Input folder '{InputDir}' doesn't exist.");
                return false;
            }

            // set outputfolder to inputfolder if not provided.
            if (string.IsNullOrEmpty(OutputDir))
            {
                OutputDir = InputDir;
            }

            if (string.IsNullOrEmpty(Stages.Converter.TemplatesDir))
            {
                MessageHelper.Error($"ERROR: Stages.Converter.TemplatesDir folder is mandatory.");
                return false;
            }

            if (!Directory.Exists(Stages.Converter.TemplatesDir))
            {
                MessageHelper.Error($"ERROR: Template folder '{Stages.Converter.TemplatesDir}' doesn't exist.");
                return false;
            }

            if (string.IsNullOrEmpty(Stages.Anonymizer.ToolConfigPath))
            {
                MessageHelper.Error($"ERROR: Stages.Anonymizer.ToolConfigPath folder is mandatory.");
                return false;
            }

            if (!File.Exists(Stages.Anonymizer.ToolConfigPath))
            {
                MessageHelper.Error($"ERROR: Anonymizer config file '{Stages.Anonymizer.ToolConfigPath}' doesn't exist.");
                return false;
            }

            return true;
        }
    }
}
