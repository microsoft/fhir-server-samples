namespace FhirIngestion.Tools.Converter.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Container for the records coming from the <see cref="Table"/>.
    /// Indexers are implemented in this class for easy access to the records.
    /// </summary>
    public class Table
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="fieldnames">List of field names in the table.</param>
        public Table(string name, params string[] fieldnames)
        {
            Precondition.Requires(!string.IsNullOrWhiteSpace(name));

            Name = name;
            FieldNames = new List<string>(fieldnames);
            Records = new Records(this);
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the list of field names in the table.
        /// </summary>
        public List<string> FieldNames { get; private set; }

        /// <summary>
        /// Gets the <see cref="Record"/> collection of the table.
        /// </summary>
        public Records Records { get; private set; }

        /// <summary>
        /// Indexer to return table with given name or null when not found.
        /// </summary>
        /// <param name="index">Index of the table in the collection.</param>
        /// <returns><see cref="Table"/> object or null.</returns>
        public Record this[int index]
        {
            get => Records[index];
        }

        /// <summary>
        /// Output verbose information on this table.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void WriteVerbose()
        {
            MessageHelper.Verbose($"Table {Name} with {Records.Count} rows has these fields:");
            foreach (string fieldname in FieldNames)
            {
                MessageHelper.Verbose($"- {fieldname}");
            }

            MessageHelper.Verbose(string.Empty);
        }
    }
}
