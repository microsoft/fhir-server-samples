namespace FhirIngestion.Tools.Common.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Anonymizer options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AnonymizerOption
    {
        /// <summary>Gets or sets a value indicating the anonymizer tool path.</summary>
        public string ToolPath { get; set; }

        /// <summary>Gets or sets a value indicating the anonymizer configuration path.</summary>
        public string ToolConfigPath { get; set; }

        /// <summary>Gets or sets a value indicating the output directory.</summary>
        public string OutputDir { get; set; }
    }
}
