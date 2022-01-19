namespace FhirIngestion.Tools.Converter.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Collection of records in a table.
    /// </summary>
    public class Records : List<Record>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Records"/> class.
        /// </summary>
        /// <param name="parent">Parent <see cref="Table"/>.</param>
        public Records(Table parent)
        {
            Precondition.NotNull(parent);

            Table = parent;
        }

        /// <summary>
        /// Gets the parent table.
        /// </summary>
        public Table Table { get; private set; }

        /// <summary>
        /// Add a record with the given values to the collection.
        /// </summary>
        /// <param name="values">List of values.</param>
        public Record AddValues(params object[] values)
        {
            Precondition.Requires(
                values.Length == Table.FieldNames.Count,
                $"New record in '{Table.Name}': {values.Length} not equals {Table.FieldNames.Count} fields.");

            Record record = new Record(Table, values);
            this.Add(record);
            return record;
        }
    }
}
