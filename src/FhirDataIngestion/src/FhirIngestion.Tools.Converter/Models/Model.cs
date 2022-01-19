namespace FhirIngestion.Tools.Converter.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Class containing the model for the parquet data.
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Gets the list of parquet tables.
        /// </summary>
        public Tables Tables { get; } = new Tables();

        /// <summary>
        /// Indexer to return table with given name or null when not found.
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <returns><see cref="Table"/> object or null.</returns>
        public Table this[string name]
        {
            get => Tables[name];
        }

        /// <summary>
        /// Verbose output of the model.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void WriteVerbose()
        {
            foreach (Table table in Tables)
            {
                table.WriteVerbose();
            }
        }
    }
}
