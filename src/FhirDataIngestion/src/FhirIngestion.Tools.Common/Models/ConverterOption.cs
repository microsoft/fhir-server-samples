namespace FhirIngestion.Tools.Common.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Convert options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConverterOption
    {
        /// <summary>Gets or sets a value indicating the template directory.</summary>
        public string TemplatesDir { get; set; }

        /// <summary>Gets or sets a value indicating the output directory.</summary>
        public string OutputDir { get; set; }
    }
}
