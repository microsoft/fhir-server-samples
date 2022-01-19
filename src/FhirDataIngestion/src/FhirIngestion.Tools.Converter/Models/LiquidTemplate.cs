namespace FhirIngestion.Tools.Converter.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Model class for storing the Liquid template.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LiquidTemplate
    {
        /// <summary>
        /// Gets or sets the filename of the liquid template.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the name of the liquid template.
        /// This is the filename without the extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the content of the liquid template.
        /// </summary>
        public string Content { get; set; }
    }
}
