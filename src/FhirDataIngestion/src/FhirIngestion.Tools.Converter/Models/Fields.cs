namespace FhirIngestion.Tools.Converter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Collection of fields.
    /// </summary>
    public class Fields : List<Field>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Fields"/> class.
        /// </summary>
        /// <param name="table">Parent <see cref="Table"/>.</param>
        public Fields(Table table)
        {
            Precondition.NotNull(table);

            Table = table;
        }

        /// <summary>
        /// Gets the table.
        /// </summary>
        public Table Table { get; private set; }

        /// <summary>
        /// Indexer on the fields collection on field name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <returns>A <see cref="Field"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the fieldname doesn't exist in the collection.</exception>
        public object this[string name]
        {
            get
            {
                Field field =
                    this.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (field != null)
                {
                    return field.Value;
                }

                return null;
            }
        }
    }
}
