namespace FhirIngestion.Tools.Common.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Stages option.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class StagesOptions
    {
        /// <summary>Gets or sets a value indicating the converter options.</summary>
        public ConverterOption Converter { get; set; }

        /// <summary>Gets or sets a value indicating the anonymizer options.</summary>
        public AnonymizerOption Anonymizer { get; set; }

        /// <summary>Gets or sets a value indicating publisher options.</summary>
        public PublisherOption Publisher { get; set; }
    }
}
