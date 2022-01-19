namespace FhirIngestion.Tools.Common.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class implementing OS platform validation methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class OSPlatformHelper
    {
        /// <summary>
        /// The supported OS Platforms.
        /// </summary>
        private static readonly OSPlatform[] SupportedOSPlatforms = { OSPlatform.Windows, OSPlatform.Linux };

        /// <summary>
        /// Ensures that Windows and Linux are the supported OS platforms.
        /// </summary>
        public static void EnsureSupportedOSPlatforms()
        {
            if (!SupportedOSPlatforms.Any(os => RuntimeInformation.IsOSPlatform(os)))
            {
                throw new PlatformNotSupportedException("Only Windows and Linux are supported OS platforms.");
            }
        }
    }
}
