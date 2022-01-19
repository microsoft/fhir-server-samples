namespace FhirIngestion.Tools.Converter.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Record class containing data in a <see cref="Fields"/> collection.
    /// </summary>
    public class Record
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Record"/> class.
        /// </summary>
        /// <param name="parent">Parent <see cref="Table"/>.</param>
        public Record(Table parent)
        {
            Precondition.NotNull(parent);

            Table = parent;
            Fields = new Fields(Table);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Record"/> class.
        /// </summary>
        /// <param name="parent">Parent <see cref="Table"/>.</param>
        /// <param name="values">List of values.</param>
        public Record(Table parent, params object[] values)
        {
            Precondition.NotNull(parent);

            Table = parent;
            Fields = new Fields(Table);

            Precondition.Requires(
                values.Length == Table.FieldNames.Count,
                $"New record in '{Table.Name}': {values.Length} not equals {Table.FieldNames.Count} fields.");

            int index = 0;
            foreach (object value in values)
            {
                Fields.Add(new Field(Table, Table.FieldNames[index++]) { Value = value });
            }
        }

        /// <summary>
        /// Gets the parent table.
        /// </summary>
        public Table Table { get; private set; }

        /// <summary>
        /// Gets the fields in the record.
        /// </summary>
        public Fields Fields { get; private set; }

        /// <summary>
        /// Indexer to return table with given name or null when not found.
        /// </summary>
        /// <param name="index">Name of the table.</param>
        /// <returns><see cref="Table"/> object or null.</returns>
        public object this[int index]
        {
            get => Fields[index].Value;
        }

        /// <summary>
        /// Indexer to return the value of the field with the given name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <returns>The value of the field.</returns>
        public object this[string name]
        {
            get => Fields[name];
        }

        /// <summary>
        /// Get only the field values from the record.
        /// </summary>
        /// <returns>Field values.</returns>
        public object[] GetValues()
        {
            List<object> values = new List<object>();
            foreach (Field field in Fields)
            {
                values.Add(field.Value);
            }

            return values.ToArray();
        }
    }
}
