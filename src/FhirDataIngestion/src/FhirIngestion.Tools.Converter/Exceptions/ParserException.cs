namespace FhirIngestion.Tools.Converter.Exceptions
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Exception class for the ParserService.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        public ParserException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">Message of exception.</param>
        public ParserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">Message of exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
