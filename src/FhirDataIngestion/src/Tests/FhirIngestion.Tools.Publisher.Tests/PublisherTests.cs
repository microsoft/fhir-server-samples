using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FhirIngestion.Tools.Common.Models;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FhirIngestion.Tools.Publisher.Tests
{
    [ExcludeFromCodeCoverage]
    public class PublisherTests
    {
        private readonly Mock<HttpMessageHandler> _handler;

        private const string FakeUri = "https://wwww.localhost.org";
        private static readonly string s_badInputPath = Path.GetFullPath("./TestFiles/Bundles/Invalid");
        private static readonly string s_emptyInputPath = Path.GetFullPath("./TestFiles/Bundles/Empty");
        private static readonly string s_bundleManyEntriesInputPath = Path.GetFullPath("./TestFiles/Bundles/Many");
        private static readonly string s_bundleSingleEntryInputPath = Path.GetFullPath("./TestFiles/Bundles/Single");
        private static readonly int s_expectedRequestsCount = GetBundleLinesCount();
        private readonly ConfigurationOption _options = GetConfigurationOption();

        public PublisherTests()
        {
            _handler = new Mock<HttpMessageHandler>();
        }

        [Fact]
        public async Task Publish_Many_Data_Success()
        {
            // Arrange
            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            // Act
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));
            var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_bundleManyEntriesInputPath);

            // Assert
            Assert.True(succcess);
            _handler.VerifyAnyRequest(Times.Exactly(s_expectedRequestsCount));
        }

        [Fact]
        public async Task Publish_Many_Data_Fails()
        {
            // Arrange
            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.BadRequest)
                .Verifiable();

            // Act, Assert
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));
            var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_bundleManyEntriesInputPath);

            Assert.False(succcess);
            _handler.VerifyAnyRequest(Times.Exactly(s_expectedRequestsCount));
        }

        [Fact]
        public async Task Publish_Invalid_Data_Fails()
        {
            // Arrange
            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            // Act, Assert
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));

            var ex = await Assert.ThrowsAsync<FormatException>(async () =>
                {
                    var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_badInputPath);
                }
            );

            Assert.IsType<JsonReaderException>(ex.InnerException);
            _handler.VerifyAnyRequest(Times.Exactly(0));
        }


        [Fact]
        public async Task Publish_EmptyEntry_Data_Fails()
        {
            // Arrange
            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            // Act, Assert
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));

            var ex = await Assert.ThrowsAsync<FormatException>(async () =>
            {
                var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_emptyInputPath);
            }
            );

            Assert.IsType<JsonReaderException>(ex.InnerException);
            _handler.VerifyAnyRequest(Times.Exactly(0));
        }

        [Fact]
        public async Task Publish_Fail_WhenNoInputDir()
        {
            // Arrange
            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.OK)
                .Verifiable();

            // Act, Assert
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));
            _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var (succcess, outputFolder) = await publisherProcess.ExecuteAsync();
            }
            );

            _handler.VerifyAnyRequest(Times.Exactly(0));
        }

        [Fact]
        public async Task Publish_Single_Data_Success_WhenBeingThrottled()
        {
            // Arrange
            _handler.SetupAnyRequestSequence()
                .ReturnsResponse(HttpStatusCode.TooManyRequests)
                .ReturnsResponse(HttpStatusCode.ServiceUnavailable)
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            using var publisherProcess = new PublisherProcess(_options, new HttpClient(_handler.Object));
            var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_bundleSingleEntryInputPath);

            // Assert
            Assert.True(succcess);
            _handler.VerifyAnyRequest(Times.Exactly(_options.Stages.Publisher.MaxRetryCount));
        }

        [Fact]
        public async Task Publish_Data_Fails_WhenInvalidAuthSettingsPassed()
        {
            // Arrange
            ConfigurationOption options = GetConfigurationOption();
            options.Stages.Publisher.AadClientId = "AadClientId";
            options.Stages.Publisher.AadClientSecret = "AadClientSecret";
            options.Stages.Publisher.AadResource = FakeUri;
            options.Stages.Publisher.AadTenantId = FakeUri + $"/{Guid.NewGuid()}";

            _handler.SetupAnyRequest()
                .ReturnsResponse(HttpStatusCode.Unauthorized)
                .Verifiable();

            // Act, Assert
            using var publisherProcess = new PublisherProcess(options, new HttpClient(_handler.Object));
            _ = await Assert.ThrowsAsync<AggregateException>(async () =>
                {
                    var (succcess, outputFolder) = await publisherProcess.ExecuteAsync(s_bundleSingleEntryInputPath);
                }
            );

            _handler.VerifyAnyRequest(Times.Exactly(0));
        }

        [Fact]
        public void Test_Environement_Overrrides()
        {
            // Arrange
            ConfigurationOption configuration = GetConfigurationOption();
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadClientIdEnvironementVariableName, "something");
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadClientSecretEnvironementVariableName, "secret");
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadTenantIdEnvironementVariableName, "tenant");
            Environment.SetEnvironmentVariable(ConfigurationOption.ApplicationInsightKeyEnvironmentVariable, "AppInsight");
            // Assert

            //Act
            Assert.Equal("something", configuration.Stages.Publisher.AadClientId);
            Assert.Equal("secret", configuration.Stages.Publisher.AadClientSecret);
            Assert.Equal("tenant", configuration.Stages.Publisher.AadTenantId);
            Assert.Equal("AppInsight", configuration.ApplicationInsightsInstrumentationKey);

            // Clean
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadClientIdEnvironementVariableName, null);
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadClientSecretEnvironementVariableName, null);
            Environment.SetEnvironmentVariable(Common.Models.PublisherOption.AadTenantIdEnvironementVariableName, null);
            Environment.SetEnvironmentVariable(ConfigurationOption.ApplicationInsightKeyEnvironmentVariable, null);
            configuration.Stages.Publisher.AadClientId = null;
            configuration.Stages.Publisher.AadClientSecret = null;
            configuration.Stages.Publisher.AadTenantId = null;
            configuration.ApplicationInsightsInstrumentationKey = null;
        }

        private static int GetBundleLinesCount()
        {
            string[] files = Directory.GetFiles(s_bundleManyEntriesInputPath, "*.ndjson", SearchOption.TopDirectoryOnly);
            int count = 0;

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                count += lines.Length;
            }

            return count;
        }

        private static ConfigurationOption GetConfigurationOption()
        {
            return new ConfigurationOption()
            {
                OutputDir = Path.GetFullPath("./TestFiles"),
                VerboseLogs = true,
                Stages = new StagesOptions()
                {
                    Publisher = new Common.Models.PublisherOption()
                    {
                        FhirServerApiUri = FakeUri,
                        MaxRetryCount = 3,
                        MaxDegreeOfParallelism = 8,
                        MetricsRefreshInterval = 3,
                        OutputResponseBundlesDir = "published"
                    }
                }
            };
        }
    }
}
