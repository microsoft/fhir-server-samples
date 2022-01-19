namespace FhirIngestion.Tools.Publisher
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Common.Models;
    using FhirIngestion.Tools.Common.Observability;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Hl7.Fhir.Specification.Source;
    using Hl7.Fhir.Validation;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json.Linq;
    using Polly;
    using Polly.Contrib.WaitAndRetry;

    /// <summary>
    /// The process for Publishing FHIR (ND)JSON files to Azure API for FHIR.
    /// Major Source: https://github.com/hansenms/FhirLoader.
    /// </summary>
    public class PublisherProcess : BaseProcess, IDisposable
    {
        // Service limits: https://docs.microsoft.com/en-us/azure/healthcare-apis/fhir/fhir-features-supported?WT.mc_id=Portal-Microsoft_Healthcare_APIs#service-limits
        private const int AzureApiForFhirMaxBundleSize = 500;
        private readonly HttpClient _httpClient;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublisherProcess"/> class.
        /// </summary>
        /// <param name="options">Command line parameters.</param>
        /// <param name="httpClient">The http client.</param>
        public PublisherProcess(ConfigurationOption options, HttpClient httpClient = null)
            : base(options)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <inheritdoc/>
        public override async Task<(bool success, string outputPath)> ExecuteAsync()
        {
            return await ExecuteAsync(string.Empty);
        }

        /// <inheritdoc/>
        public override async Task<(bool success, string outputPath)> ExecuteAsync(string inputFolder)
        {
            // Initialize publisher metrics
            string line;
            long validNdJsonLinesCount = 0;
            long inValidNdJsonLinesCount = 0;
            MetricsCollector metrics = new MetricsCollector();
            string outputPath = RecreateOutputDirectory();

            // initialize validator settings
            Validator validator = InitializeValidator();
            FhirJsonParser fhirJsonParser = new FhirJsonParser();

            // Prepare the Dataflow ActionBlock to publish the data
            ActionBlock<string> actionBlock = GetActionBlock(metrics, outputPath);

            // Read the ndjson files and feed it to the threads
            string[] files = GetNdJsonFilesForPublishing(inputFolder);

            // Start stopwatch and collecting publisher metrics
            metrics.Start(Options.Stages.Publisher.MetricsRefreshInterval);

            foreach (var file in files)
            {
                using var buffer = new StreamReader(file);

                while ((line = buffer.ReadLine()) != null)
                {
                    // check if bundle is valid
                    bool isValid = ValidateBundle(line, validator, fhirJsonParser);
                    if (isValid)
                    {
                        actionBlock.Post(line);
                        validNdJsonLinesCount++;
                    }
                    else
                    {
                        MessageHelper.Error("Bundle is not validated by Bundle Profile");
                        inValidNdJsonLinesCount++;
                    }
                }
            }

            // Wait until all is done
            actionBlock.Complete();
            actionBlock.Completion.Wait();

            // Stop metrics collection and stopwatch
            metrics.Stop();

            ApplicationInfoTelemetry.TrackMetric("PublisherProcessValidLines", metrics.TotalSuccessRequests);
            ApplicationInfoTelemetry.TrackMetric("PublisherProcessInvalidLines", inValidNdJsonLinesCount);
            ApplicationInfoTelemetry.TrackMetric("PublisherProcessResourceFiles", files.Length);

            MessageHelper.Verbose($"Finished processing {files.Length} bulk data resource file(s).");
            MessageHelper.Verbose($"Total invalid ndjson line(s) : {inValidNdJsonLinesCount}.");
            MessageHelper.Verbose($"Total success requests: {metrics.TotalSuccessRequests} of {validNdJsonLinesCount} in {metrics.StopwatchElapsed}.");

            // The definition of success is when all resources where successfuly published
            bool success = metrics.TotalSuccessRequests == validNdJsonLinesCount;

            return await System.Threading.Tasks.Task.FromResult((success, outputPath));
        }

        /// <summary>
        /// Disposes the unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The bulk of the clean-up code of disposing.
        /// </summary>
        /// <param name="disposing">disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // free managed resources
                _httpClient.Dispose();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Gets a new DataFlow ActionBlock with the publisher logic.
        /// </summary>
        /// <param name="metrics">the metricsCollector to track the publishing progress.</param>
        /// <param name="outputPath">the outputPath where response files will be stored.</param>
        /// <returns>DataFlow ActionBlock.</returns>
        private ActionBlock<string> GetActionBlock(MetricsCollector metrics, string outputPath)
        {
            PublisherOption publisherOptions = Options.Stages.Publisher;
            Uri fhirServerUri = ParseUri(publisherOptions.FhirServerApiUri);
            Uri aadResource = ParseUri(publisherOptions.AadResource);
            Uri authority = ParseUri(publisherOptions.AadTenantId);
            TimeSpan medianFirstRetryDelay = TimeSpan.FromSeconds(publisherOptions.MedianFirstRetryDelay);

            bool useAuth = authority != null && publisherOptions.AadClientId != null && publisherOptions.AadClientSecret != null;
            AuthenticationContext authContext = useAuth ? new AuthenticationContext(authority?.AbsoluteUri, new TokenCache()) : null;
            ClientCredential clientCredential = useAuth ? new ClientCredential(publisherOptions.AadClientId, publisherOptions.AadClientSecret) : null;

            return new ActionBlock<string>(
                        async resourceString =>
                        {
                            var content = new StringContent(resourceString, Encoding.UTF8, "application/json");
                            var pollyDelays = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay, publisherOptions.MaxRetryCount);

                            HttpResponseMessage uploadResult = await Policy
                                .HandleResult<HttpResponseMessage>(response => (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.ServiceUnavailable))
                                .WaitAndRetryAsync(pollyDelays, (result, timeSpan, retryCount, context) =>
                                {
                                    if (retryCount > 0)
                                    {
                                        MessageHelper.Verbose($"Request failed with {result.Result.StatusCode}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                                    }
                                })
                                .ExecuteAsync(() =>
                                {
                                    var message = new HttpRequestMessage(HttpMethod.Post, fhirServerUri);

                                    message.Content = content;

                                    if (useAuth)
                                    {
                                        var authResult = authContext.AcquireTokenAsync(aadResource.AbsoluteUri.TrimEnd('/'), clientCredential).Result;
                                        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                                    }

                                    return _httpClient.SendAsync(message);
                                });

                            metrics.Collect(DateTime.Now, uploadResult.IsSuccessStatusCode);

                            string resultContent = await uploadResult.Content.ReadAsStringAsync();

                            // We opt to only inform !SuccessStatusCode to further help troubleshooting in Application Insights
                            if (!uploadResult.IsSuccessStatusCode && Options.VerboseLogs)
                            {
                                MessageHelper.Error($"Publish error (StatusCode: {uploadResult.StatusCode}): {resultContent}");
                            }

                            // Complete responses will be stored in as JSON file, with a random Guid
                            string outputResponseFilePath = Path.Combine(outputPath, $"{Guid.NewGuid()}.json");
                            await File.WriteAllTextAsync(outputResponseFilePath, resultContent);
                        },
                        new ExecutionDataflowBlockOptions
                        {
                            MaxDegreeOfParallelism = publisherOptions.MaxDegreeOfParallelism,
                        });
        }

        /// <summary>
        /// Initialize FHIR Validator with profiles in `Profiles` folder.
        /// </summary>
        private Validator InitializeValidator()
        {
            // create profile source using `Profiles` folder
            var profileSource = new CachedResolver(new MultiResolver(
                new DirectorySource(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles"),
                    new DirectorySourceSettings() { IncludeSubDirectories = true }), ZipSource.CreateValidationSource()));

            // set validation settings
            var validationSettings = new ValidationSettings
            {
                ResourceResolver = profileSource,
                GenerateSnapshot = true,
                Trace = false,
                ResolveExternalReferences = false,
            };

            // create and return validator
            Validator validator = new Validator(validationSettings);
            return validator;
        }

        /// <summary>
        /// Validates the Bundle content using profiles.
        /// </summary>
        /// <param name="bundleLine">One line string version of bundle JSON file.</param>
        /// <param name="validator">Validator object initilazed using InitialzeValidator() method.</param>
        /// <param name="fhirParser">FHIR Parser object.</param>
        private bool ValidateBundle(string bundleLine, Validator validator, FhirJsonParser fhirParser)
        {
            // parse bundle payload using FHIRJsonParser
            string strBundle = bundleLine;
            Bundle bundle = fhirParser.Parse<Bundle>(strBundle);

            // validate bundle
            var resultBundle = validator.Validate(bundle);

            if (Options.VerboseLogs)
            {
                // print invalid cases
                if (!resultBundle.Success)
                {
                    MessageHelper.Error($"Bundle validation error: {resultBundle.Issue[0]}");
                }

                if (bundle.Entry.Count > AzureApiForFhirMaxBundleSize)
                {
                    MessageHelper.Warning($"Bundle entries length is {bundle.Entry.Count}, exceeds the service limits (max: {AzureApiForFhirMaxBundleSize}).");
                }
            }

            return resultBundle.Success;
        }

        private Uri ParseUri(string uri)
        {
            return string.IsNullOrWhiteSpace(uri) ? (Uri)null : new Uri(uri);
        }

        private string[] GetNdJsonFilesForPublishing(string inputFolder)
        {
            return Directory.GetFiles(inputFolder, "*.ndjson", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Recreates the output directory, cleanning up all content from previous runs.
        /// </summary>
        /// <returns>the output director path.</returns>
        private string RecreateOutputDirectory()
        {
            string outputDirPath = Path.Combine(Options.OutputDir, Options.Stages.Publisher.OutputResponseBundlesDir);

            if (Directory.Exists(outputDirPath))
            {
                Directory.Delete(outputDirPath, true);
            }

            Directory.CreateDirectory(outputDirPath);

            return outputDirPath;
        }
    }
}
