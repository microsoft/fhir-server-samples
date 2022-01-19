namespace FhirIngestion.Tools.Common.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Indicates to Code Analysis that a method validates a particular parameter.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatedNotNullAttribute"/> class.
        /// </summary>
        public ValidatedNotNullAttribute()
        {
        }
    }
}
