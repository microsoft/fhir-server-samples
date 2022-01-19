using FhirIngestion.Tools.Common.Helpers;
using Xunit;

namespace FhirIngestion.Tools.Common.Tests
{
    public class HelperTests
    {
        [Fact]
        public void PathHelper_Tests()
        {
            // ASSERT
            Assert.Equal("valid-filename", PathHelpers.SanitizeFilenameToTablename("valid-filename"));
            Assert.Equal("filename-with-spaces", PathHelpers.SanitizeFilenameToTablename("filename with spaces"));
            Assert.Equal("file_name-12345-67890", PathHelpers.SanitizeFilenameToTablename("file_name !@#$%^&()+=12345-67890"));
        }
    }
}
