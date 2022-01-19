namespace FhirIngestion.Tools.App
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Anonymizer;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Common.Models;
    using FhirIngestion.Tools.Common.Observability;
    using FhirIngestion.Tools.Converter;
    using FhirIngestion.Tools.Converter.Models;
    using FhirIngestion.Tools.Converter.Services;
    using FhirIngestion.Tools.Publisher;
    using CommandLine;
    using Microsoft.ApplicationInsights.DataContracts;
    using Newtonsoft.Json;

    /// <summary>
    /// Main entry class for the console application.
    /// </summary>
    [ExcludeFromCodeCoverage]
#pragma warning disable S1118 // Utility classes should not have public constructors
    internal class Program
#pragma warning restore S1118 // Utility classes should not have public constructors
    {
        private static int returnvalue;

        /// <summary>
        /// Main entry point of the application.
        /// </summary>
        /// <param name="args">Commandline parameters.</param>
        public static async Task<int> Main(string[] args)
        {
            OSPlatformHelper.EnsureSupportedOSPlatforms();

            await Parser.Default.ParseArguments<CommandlineOptions>(args)
                .WithNotParsed(HandleErrors)
                .WithParsedAsync<CommandlineOptions>(RunLogic);

            Console.WriteLine($"Exit with return code {returnvalue}");

            // Flush logger and wait for data to be sent
            ApplicationInfoTelemetry.Flush();

            return returnvalue;
        }

        /// <summary>
        /// Handle errors in commandline parsing.
        /// </summary>
        /// <param name="errors">List of errors.</param>
        private static void HandleErrors(IEnumerable<Error> errors)
        {
            returnvalue = 1;
        }

        /// <summary>
        /// The main logic of the console application.
        /// </summary>
        /// <param name="options">Commandline options.</param>
        private static async Task RunLogic(CommandlineOptions options)
        {
            // Default to error returnvalue
            returnvalue = 1;

            if (!options.IsValid())
            {
                return;
            }

            ConfigurationOption configuration = JsonConvert.DeserializeObject<ConfigurationOption>(File.ReadAllText(options.ConfigurationFile));

            if (!configuration.IsValid())
            {
                return;
            }

            try
            {
                // Initialize Logger
                ApplicationInfoTelemetry.Initialize(configuration.ApplicationInsightsInstrumentationKey);

                // Initialize AppInsights main operation
                using var appRunOperation = ApplicationInfoTelemetry.StartOperation("AppRun");

                // Send current config to AppInsights
                EventTelemetry configTelemetry = new EventTelemetry("AppRunConfiguration");
                configTelemetry.Properties.Add("config", File.ReadAllText(options.ConfigurationFile));
                ApplicationInfoTelemetry.TrackEvent(configTelemetry);

                // Stage 1: Import data and convert to FHIR JSONs
                using var converterOperation = ApplicationInfoTelemetry.StartDependencyOperation("Converter");
                var converter = new ConverterProcess(configuration);
                var (converterProcessSuccess, converterOutputFolder) = await converter.ExecuteAsync();
                ApplicationInfoTelemetry.StopOperation(converterOperation);

                if (!converterProcessSuccess)
                {
                    return;
                }

                // Stage 2: Anonymize the converted FHIR JSONs
                using var anonymizerOperation = ApplicationInfoTelemetry.StartDependencyOperation("Anonymizer");
                var anonymizer = new AnonymizeProcess(configuration);
                var (anonymizerProcessSuccess, anonymizerOutputFolder) = await anonymizer.ExecuteAsync(converterOutputFolder);
                ApplicationInfoTelemetry.StopOperation(anonymizerOperation);

                if (!anonymizerProcessSuccess)
                {
                    return;
                }

                // Stage 3: Publish the anonymized FHIR JSONs
                using var publisherOperation = ApplicationInfoTelemetry.StartDependencyOperation("Publisher");
                using var publisher = new PublisherProcess(configuration);
                var (publisherProcessSuccess, publisherOutputFolder) = await publisher.ExecuteAsync(anonymizerOutputFolder);
                ApplicationInfoTelemetry.StopOperation(publisherOperation);

                if (!publisherProcessSuccess)
                {
                    return;
                }

                // If we get to this point, then all stages completed with success.
                returnvalue = 0;

                ApplicationInfoTelemetry.StopOperation(appRunOperation);
            }
            catch (Exception ex)
            {
                ApplicationInfoTelemetry.TrackException(new ExceptionTelemetry(ex));
                if (configuration.VerboseLogs)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
