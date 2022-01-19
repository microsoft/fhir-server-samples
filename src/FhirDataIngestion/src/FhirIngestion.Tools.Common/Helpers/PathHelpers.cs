namespace FhirIngestion.Tools.Common.Helpers
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Path helper methods.
    /// </summary>
    public static class PathHelpers
    {
        /// <summary>
        /// Sanitize a given file name to a valid table name.
        /// We will remove anything not alphabetic or alphanumeric or a -.
        /// It will replace spaces by a -.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns>Sanitized table name.</returns>
        public static string SanitizeFilenameToTablename(string filename)
        {
            return Regex.Replace(filename, @"[^\w\- ]", string.Empty).Replace(" ", "-");
        }
    }
}
