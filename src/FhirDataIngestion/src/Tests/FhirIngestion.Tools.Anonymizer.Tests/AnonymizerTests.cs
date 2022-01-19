using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FhirIngestion.Tools.Common.Models;
using Newtonsoft.Json;
using Xunit;

namespace FhirIngestion.Tools.Anonymizer.Tests
{
    [ExcludeFromCodeCoverage]
    public class AnonymizerTests
    {
        [Fact]
        public async Task Run_Anonymizer_Process_WithInvalidOptions_Not_Successfull()
        {
            // ARRANGE
            var options = new ConfigurationOption() { VerboseLogs = true };

            // ACT, ASSERT
            var anonymizeProcess = new AnonymizeProcess(options);

            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    var (result, outputFolder) = await anonymizeProcess.ExecuteAsync();
                }
            );
        }

        [Fact]
        public async Task Run_Anonymizer_Process_WithInvalidToolPathOptions_Not_Successfull()
        {
            // ARRANGE
            string currentDirectory = Directory.GetCurrentDirectory();

            var options = new ConfigurationOption()
            {
                VerboseLogs = true,
                OutputDir = currentDirectory,
                InputDir = currentDirectory,
                Stages = new StagesOptions()
                {
                    Anonymizer = new Common.Models.AnonymizerOption()
                    {
                        ToolPath = currentDirectory,
                        ToolConfigPath = currentDirectory,
                        OutputDir = currentDirectory
                    }
                }
            };

            // ACT, ASSERT
            var anonymizeProcess = new AnonymizeProcess(options);
            var (result, outputFolder) = await anonymizeProcess.ExecuteAsync(currentDirectory);
            Assert.False(result);
        }
    }
}
