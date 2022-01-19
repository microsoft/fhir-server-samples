namespace FhirIngestion.Tools.Common.Models
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using FhirIngestion.Tools.Common.Helpers;
    using CommandLine;

    /// <summary>
    /// Class for command line options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CommandlineOptions
    {
        /// <summary>
        /// Gets or sets the folder with documents.
        /// </summary>
        [Option('c', "config", Required = true, HelpText = "Configuration file as a json file.")]
        public string ConfigurationFile { get; set; }

        /// <summary>
        /// Check if options are valid.
        /// </summary>
        /// <returns>Valid true/false.</returns>
        public bool IsValid()
        {
            if (!File.Exists(ConfigurationFile))
            {
                MessageHelper.Error($"ERROR: Input configuration file '{ConfigurationFile}' doesn't exist.");
                return false;
            }

            return true;
        }
    }
}
