namespace FhirIngestion.Tools.Common.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Precondition helper methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "CA1303", Justification = "Using fixed strings, because this is a helper class.")]
    public static class Precondition
    {
        /// <summary>
        /// Check if condition is met, otherwise an ArgumentException is thrown.
        /// </summary>
        /// <param name="value">Object value to check.</param>
        [ValidatedNotNull]
        public static void NotNull([ValidatedNotNull] object value)
        {
            NotNull(value, "Required condition is not met.");
        }

        /// <summary>
        /// Check if condition is met, otherwise an ArgumentException is thrown with the given message.
        /// </summary>
        /// <param name="value">Object value to check.</param>
        /// <param name="message">Message to show in exception when condition is FALSE.</param>
        [ValidatedNotNull]
        public static void NotNull([ValidatedNotNull] object value, string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        /// <summary>
        /// Check if condition is met, otherwise an ArgumentException is thrown.
        /// </summary>
        /// <param name="condition">Condition to check.</param>
        public static void Requires(bool condition)
        {
            Requires(condition, "Required condition is not met.");
        }

        /// <summary>
        /// Check if condition is met, otherwise an ArgumentException is thrown with the given message.
        /// </summary>
        /// <param name="condition">Condition to check.</param>
        /// <param name="message">Message to show in exception when condition is FALSE.</param>
        public static void Requires(bool condition, string message)
        {
            if (condition == false)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
