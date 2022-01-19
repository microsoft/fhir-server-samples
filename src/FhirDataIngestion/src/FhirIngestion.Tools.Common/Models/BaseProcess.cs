namespace FhirIngestion.Tools.Common.Models
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    /// <summary>
    /// Base process class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class BaseProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseProcess"/> class.
        /// </summary>
        /// <param name="options">Command line parameters.</param>
        public BaseProcess(ConfigurationOption options)
        {
            Options = options;
        }

        /// <summary>
        /// Gets the command line parameters.
        /// </summary>
        protected internal ConfigurationOption Options { get; private set; }

        /// <summary>
        /// Execute the logic of this process.
        /// </summary>
        /// <returns>Success true/false and the process output folder.</returns>
        public abstract Task<(bool success, string outputPath)> ExecuteAsync();

        /// <summary>
        /// Execute the logic of this process.
        /// </summary>
        /// <returns>Success true/false and the process output folder.</returns>
        public abstract Task<(bool success, string outputPath)> ExecuteAsync(string inputFolder);
    }
}
