namespace FhirIngestion.Tools.Converter.Tests
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Models;
    using Newtonsoft.Json;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class ConverterTests
    {
        [Fact]
        public async Task Run_Converter_Process_Successfull()
        {
            // ARRANGE
            var configuration = new ConfigurationOption()
            {
                VerboseLogs = true,
                InputDir = Path.GetFullPath("./TestFiles"),
                OutputDir = Path.GetFullPath("."),
                Stages = new StagesOptions()
                {
                    Converter = new ConverterOption()
                    {
                        TemplatesDir = Path.GetFullPath("./TestFiles/ValidTemplates")
                    }
                }
            };

            // ACT
            ConverterProcess process = new ConverterProcess(configuration);
            var (result, outputFolder) = await process.ExecuteAsync();

            // ASSERT
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(outputFolder, "./Template1.ndjson")));
            Assert.True(File.Exists(Path.Combine(outputFolder, "./Template2.ndjson")));
        }

        [Fact]
        public async Task Run_Converter_Process_With_Template_Error_Fails()
        {
            // ARRANGE
            var configuration = new ConfigurationOption()
            {
                VerboseLogs = true,
                InputDir = Path.GetFullPath("./TestFiles"),
                OutputDir = Path.GetFullPath("."),
                Stages = new StagesOptions()
                {
                    Converter = new ConverterOption()
                    {
                        TemplatesDir = Path.GetFullPath("./TestFiles/InvalidTemplates")
                    }
                }
            };

            // ACT
            ConverterProcess process = new ConverterProcess(configuration);
            var (result, outputFolder) = await process.ExecuteAsync();

            // ASSERT
            Assert.False(result);
            Assert.False(File.Exists(Path.Combine(outputFolder, "./InvalidTemplate1.ndjson")));
        }
    }
}
