namespace FhirIngestion.Tools.Converter.Services
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using FhirIngestion.Tools.Common.Helpers;
    using FhirIngestion.Tools.Common.Observability;
    using FhirIngestion.Tools.Converter.Models;
    using Parquet;

    /// <summary>
    /// The service to read the parquet files.
    /// </summary>
    public class ParquetService
    {
        /// <summary>
        /// Delegate for notify caller which file is being read.
        /// </summary>
        /// <param name="filename">Filename being read.</param>
        public delegate void FileNotifierDelegate(string filename);

        /// <summary>
        /// Public event being fired when a file is imported.
        /// </summary>
        public event FileNotifierDelegate ImportingFile;

        /// <summary>
        /// Import all parquet files in the given folder.
        /// </summary>
        /// <param name="model">Model to add data to.</param>
        /// <param name="folder">Folder containing all the related parquet files.</param>
        public void ImportFiles(Model model, string folder)
        {
            Precondition.NotNull(model);
            Precondition.NotNull(folder);

            // search is case-insensitive by default
            string[] files = Directory.GetFiles(folder, "*.parquet");
            foreach (string file in files)
            {
                ImportingFile?.Invoke(file);

                using Stream fileStream = File.OpenRead(file);
                using ParquetReader parquetReader = new ParquetReader(fileStream);
                Parquet.Data.Rows.Table parquetTable = parquetReader.ReadAsTable();

                // create the table with the name of the file and fields from the schema.
                Parquet.Data.DataField[] parquetFields = parquetTable.Schema.GetDataFields();
                List<string> fieldnames = new List<string>();
                foreach (Parquet.Data.DataField parquetField in parquetFields)
                {
                    fieldnames.Add(parquetField.Name);
                }

                Table table = model.Tables.Add(
                    PathHelpers.SanitizeFilenameToTablename(Path.GetFileNameWithoutExtension(file)),
                    fieldnames.ToArray());

                // read all records and add to the table
                foreach (Parquet.Data.Rows.Row parquetRow in parquetTable)
                {
                    table.Records.AddValues(parquetRow.Values);
                }

                ApplicationInfoTelemetry.TrackMetric($"ParquetServiceImportRows-{table.Name}", table.Records.Count);
            }

            ApplicationInfoTelemetry.TrackMetric("ParquetServiceImportFiles", files.Length);
        }

        /// <summary>
        /// Export it.
        /// </summary>
        /// <param name="table">Table.</param>
        /// <param name="file">Filename.</param>
        [ExcludeFromCodeCoverage]
        public void ExportFile(Table table, string file)
        {
            Precondition.NotNull(table);
            Precondition.Requires(table.Records.Any());
            Precondition.NotNull(file);

            // For the schema get each field name, and determine type from the first record
            List<Parquet.Data.DataField> parquetFields = new List<Parquet.Data.DataField>();
            for (int i = 0; i < table.FieldNames.Count; i++)
            {
                parquetFields.Add(new Parquet.Data.DataField(table.FieldNames[i], table.Records[0][i].GetType()));
            }

            // Create the table with the schema and the data
            Parquet.Data.Rows.Table parquetTable = new Parquet.Data.Rows.Table(new Parquet.Data.Schema(parquetFields));
            foreach (Record record in table.Records)
            {
                parquetTable.Add(record.GetValues());
            }

            ApplicationInfoTelemetry.TrackMetric("ParquetServiceExportRows", parquetTable.Count);

            // write the parquet table to the give file.
            using Stream stream = File.OpenWrite(file);
            using ParquetWriter writer = new ParquetWriter(parquetTable.Schema, stream);
            writer.Write(parquetTable);
        }
    }
}
