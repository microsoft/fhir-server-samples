namespace FhirIngestion.Tools.Converter
{
    using System.IO;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Common.Models;
    using FhirIngestion.Tools.Common.Observability;
    using FhirIngestion.Tools.Converter.Exceptions;
    using FhirIngestion.Tools.Converter.Models;
    using FhirIngestion.Tools.Converter.Services;

    /// <summary>
    /// The process of converting input data to FHIR documents.
    /// </summary>
    public class ConverterProcess : BaseProcess
    {
        private const string OutputSubFolder = "converted";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterProcess"/> class.
        /// </summary>
        /// <param name="options">Command line parameters.</param>
        public ConverterProcess(ConfigurationOption options)
            : base(options)
        {
        }

        /// <inheritdoc/>
        public override async Task<(bool success, string outputPath)> ExecuteAsync()
        {
            return await ExecuteAsync(string.Empty);
        }

        /// <inheritdoc/>
        public override Task<(bool success, string outputPath)> ExecuteAsync(string inputFolder)
        {
            bool success = true;

            Model model = new Model();

            // read all parquet files first
            ParquetService parquet = new ParquetService();
            if (Options.VerboseLogs)
            {
                parquet.ImportingFile += (path) =>
                {
                    MessageHelper.Verbose($"Importing {Path.GetFileName(path)}");
                };
                MessageHelper.Verbose($"Importing parquet files from {Options.InputDir}.");
            }

            parquet.ImportFiles(model, Options.InputDir);

            // read all CSV files next
            CSVService csv = new CSVService();
            if (Options.VerboseLogs)
            {
                csv.ImportingFile += (path) =>
                {
                    MessageHelper.Verbose($"Importing {Path.GetFileName(path)}");
                };
                MessageHelper.Verbose($"Importing CSV files from {Options.InputDir}.");
            }

            csv.ImportFiles(model, Options.InputDir);

            if (Options.VerboseLogs)
            {
                model.WriteVerbose();
            }

            // read all Liquid templates
            TemplateService templates = new TemplateService();
            if (Options.VerboseLogs)
            {
                templates.ReadingFile += (path) =>
                {
                    MessageHelper.Verbose($"Reading {Path.GetFileName(path)}");
                };
                MessageHelper.Verbose($"Reading templates from {Options.Stages.Converter.TemplatesDir}");
            }

            templates.ReadAllTemplates(Options.Stages.Converter.TemplatesDir);

            if (Options.VerboseLogs)
            {
                templates.WriteVerbose();
            }

            // Process templates with data
            string outputFolder = Path.Combine(Options.OutputDir, OutputSubFolder);

            if (!Directory.Exists(outputFolder))
            {
                if (Options.VerboseLogs)
                {
                    MessageHelper.Verbose($"Creating {outputFolder}.");
                }

                Directory.CreateDirectory(outputFolder);
            }

            ParserService parser = new ParserService();

            if (Options.VerboseLogs)
            {
                MessageHelper.Verbose($"Writing JSON files in {outputFolder}.");
            }

            int nrOfSuccesses = 0, nrOfFailures = 0;
            foreach (LiquidTemplate template in templates.Templates)
            {
                string outputFilePath = Path.Combine(outputFolder, $"{template.Name}.ndjson");
                if (Options.VerboseLogs)
                {
                    MessageHelper.Verbose($"Writing {Path.GetFileName(outputFilePath)}");
                }

                try
                {
                    string content = parser.Render(model, template.Content, Options.Stages.Converter.TemplatesDir);
                    File.WriteAllText(outputFilePath, content);
                    nrOfSuccesses++;
                }
                catch (ParserException ex)
                {
                    MessageHelper.Error($"ERROR parsing {template.Name}: {ex.Message}");

                    ApplicationInfoTelemetry.TrackException(new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(ex));

                    nrOfFailures++;
                    success = false;
                }
            }

            // Track metrics to AppInsights
            ApplicationInfoTelemetry.TrackMetric("ConverterProcessTemplatesSucceeded", nrOfSuccesses);
            ApplicationInfoTelemetry.TrackMetric("ConverterProcessTemplatesFailes", nrOfFailures);

            return Task.FromResult((success, outputFolder));
        }
    }
}
