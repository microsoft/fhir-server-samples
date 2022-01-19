namespace FhirIngestion.Tools.Converter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Table collection class.
    /// </summary>
    public class Tables : List<Table>
    {
        /// <summary>
        /// Indexer to get a table by name from the collection.
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <returns>A <see cref="Table"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When a table with the name doesn't exist in the collection.</exception>
        public Table this[string name]
        {
            get
            {
                Table table =
                    this.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (table != null)
                {
                    return table;
                }

                return null;
            }
        }

        /// <summary>
        /// Add a table with the given name to the collection. It's checked that it doesn't exist yet.
        /// </summary>
        /// <param name="name">Table name.</param>
        /// <param name="fieldnames">List of field names.</param>
        /// <exception cref="ApplicationException">When the table name already exists.</exception>
        public Table Add(string name, params string[] fieldnames)
        {
            Precondition.Requires(!string.IsNullOrWhiteSpace(name));
            Precondition.Requires(fieldnames.Length > 0);

            if (this.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) != null)
            {
                throw new ApplicationException($"Table '{name}' already exists.");
            }

            Table table = new Table(name, fieldnames);
            Add(table);
            return table;
        }
    }
}
