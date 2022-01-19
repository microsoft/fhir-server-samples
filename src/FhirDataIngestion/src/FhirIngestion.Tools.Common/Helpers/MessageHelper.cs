namespace FhirIngestion.Tools.Common.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FhirIngestion.Tools.Common.Observability;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Helper methods to write messages to the console.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MessageHelper
    {
        /// <summary>
        /// Helper method for verbose messages.
        /// </summary>
        /// <param name="message">Message to show in verbose mode.</param>
        public static void Verbose(string message)
        {
            ApplicationInfoTelemetry.LogMessage(message, LogLevel.Information);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Helper method for warning messages.
        /// </summary>
        /// <param name="message">Message to show in verbose mode.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "We want same access for all methods.")]
        public static void Warning(string message)
        {
            ApplicationInfoTelemetry.LogMessage(message, LogLevel.Warning);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Helper method for error messages.
        /// </summary>
        /// <param name="message">Message to show in verbose mode.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "We want same access for all methods.")]
        public static void Error(string message)
        {
            ApplicationInfoTelemetry.LogMessage(message, LogLevel.Error);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }
}
