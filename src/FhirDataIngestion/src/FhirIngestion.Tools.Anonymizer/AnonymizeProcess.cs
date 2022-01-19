namespace FhirIngestion.Tools.Anonymizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Common.Models;
    using static SimpleExec.Command;

    /// <summary>
    /// The process for Anonymizeing FHIR JSON files.
    /// </summary>
    public class AnonymizeProcess : BaseProcess
    {
        private const string AnonymizeToolAppName = "Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool";
        private const string AnonymizeToolOutputSubFolderName = "anonymized";

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymizeProcess"/> class.
        /// </summary>
        /// <param name="options">Command line parameters.</param>
        public AnonymizeProcess(ConfigurationOption options)
            : base(options)
        {
        }

        /// <inheritdoc/>
        public override async Task<(bool success, string outputPath)> ExecuteAsync()
        {
            return await ExecuteAsync(string.Empty);
        }

        /// <inheritdoc/>
        public override async Task<(bool success, string outputPath)> ExecuteAsync(string inputFolder)
        {
            bool success = true;
            string outputSubFolder = Path.Combine(Options.OutputDir, AnonymizeToolOutputSubFolderName);
            string anonymizeToolAppFolder = Options.Stages.Anonymizer.ToolPath;
            string args = $"-i {inputFolder} -o {outputSubFolder} -c {Options.Stages.Anonymizer.ToolConfigPath} -v {Options.VerboseLogs} -b --validateInput true --validateOutput true";
            string result = string.Empty;
            int exitCode = -1;
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
            Func<int, bool> handleExitCodeFunc = code => (exitCode = code) < 8;
#pragma warning restore S1121 // Assignments should not be made from within sub-expressions

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result = Read("cmd", args: $"/c {AnonymizeToolAppName} {args}", anonymizeToolAppFolder, handleExitCode: handleExitCodeFunc);
                }
                else
                {
                    // use a relative path to the project root in FileName, because processStart.UseShellExecute = false in SimpleExec
                    string cmd = Path.Combine(anonymizeToolAppFolder, AnonymizeToolAppName);
                    result = Read(cmd, args, handleExitCode: handleExitCodeFunc);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }

            if (exitCode != 0)
            {
                success = false;
            }

            if (Options.VerboseLogs)
            {
                MessageHelper.Verbose(result);
            }

            return await Task.FromResult((success, outputSubFolder));
        }
    }
}
