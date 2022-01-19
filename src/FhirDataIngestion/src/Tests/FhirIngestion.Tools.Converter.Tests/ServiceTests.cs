using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FhirIngestion.Tools.Converter.Exceptions;
using FhirIngestion.Tools.Converter.Models;
using FhirIngestion.Tools.Converter.Services;
using FhirIngestion.Tools.Converter.Tests.Helpers;
using Xunit;

namespace FhirIngestion.Tools.Converter.Tests
{
    [ExcludeFromCodeCoverage]
    public class ServiceTests
    {
        [Fact]
        public void Read_Parquet_Testfiles_successfull()
        {
            // ARRANGE
            int importCounter = 0;

            Model model = new Model();
            ParquetService parquet = new ParquetService();
            parquet.ImportingFile += (file) => { importCounter++; };

            // ACT
            parquet.ImportFiles(model, "./TestFiles");

            // ASSERT
            Assert.Equal(2, importCounter);
            Assert.Equal(2, model.Tables.Count);
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table1")));
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table2")));
        }

        [Fact]
        public void Read_CSV_Testfiles_successfull()
        {
            // ARRANGE
            int importCounter = 0;

            Model model = new Model();
            CSVService service = new CSVService();
            service.ImportingFile += (file) => { importCounter++; };

            // ACT
            service.ImportFiles(model, "./TestFiles");

            // ASSERT
            Assert.Equal(2, importCounter);
            Assert.Equal(2, model.Tables.Count);
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table3")));
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table4")));
        }

        [Fact]
        public void Different_datafiles_same_name_throws()
        {
            // ARRANGE
            int importCounter = 0;

            Model model = new Model();
            ParquetService parquet = new ParquetService();
            parquet.ImportingFile += (file) => { importCounter++; };
            CSVService csv = new CSVService();
            csv.ImportingFile += (file) => { importCounter++; };

            // ACT
            parquet.ImportFiles(model, "./TestFiles/InvalidDataset");

            // ASSERT
            Assert.Throws<ApplicationException>(() => csv.ImportFiles(model, "./TestFiles/InvalidDataset"));
            Assert.Equal(2, importCounter);
            Assert.Single(model.Tables);
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table1")));
        }

        [Fact]
        public void Import_CSV_invalid_data_throws()
        {
            // ARRANGE
            int importCounter = 0;

            Model model = new Model();
            CSVService csv = new CSVService();
            csv.ImportingFile += (file) => { importCounter++; };

            // ACT

            // ASSERT
            Assert.Throws<ArgumentException>(() => csv.ImportFiles(model, "./TestFiles/InvalidDataset"));
            Assert.Equal(1, importCounter); // one file imported
            Assert.Single(model.Tables); // table created
            Assert.True(model.Tables.Exists(t => t.Name.Equals("Table1"))); // table created for this file
            Assert.Single(model.Tables[0].Records); // only first line add, second is invalid and stops proces
        }

        [Fact]
        public void Read_Liquid_Templates_successfull()
        {
            // ARANGE
            int templateCounter = 0;

            TemplateService templates = new TemplateService();
            templates.ReadingFile += (file) => { templateCounter++; };

            // ACT
            templates.ReadAllTemplates("./TestFiles/ValidTemplates");

            // ASSERT
            Assert.Equal(2, templateCounter);
            Assert.Equal(2, templates.Templates.Count);
            Assert.Equal("Template1.liquid", templates.Templates[0].Filename);
            Assert.Equal("Template1", templates.Templates[0].Name);
            Assert.Equal("Template2.liquid", templates.Templates[1].Filename);
            Assert.Equal("Template2", templates.Templates[1].Name);
        }

        [Fact]
        public void Parser_empty_parameters()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            // ACT
            string result = parser.Render(null, null);

            // ASSERT
            Assert.Equal("", result);
        }

        [Fact]
        public void Parser_with_template_using_indexing()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            Model model = new Model();
            Table table1 = DataHelper.AddRandomTable(model, "Table1");
            Table table2 = DataHelper.AddRandomTable(model, "Table2");

            // ACT
            string templateContent =
@"{{ model.Tables[""table1""].Name }}
{{ model[""table2""].Name }}
{{ model.Table1.Name }}
{{ model.Tables[""table2""].Records[2].Fields[""name""] }}
{{ model.Tables[""table2""].Records[2][""name""] }}
{{ model[""table2""][2][""name""] }}
{{ model[""table2""][2].Name }}
{{ model.Table2[2].Name }}
{{ model.Table1.Records[0].Table.Name }}";

            string predictedResult = $"{table1.Name}{Environment.NewLine}";
            predictedResult += $"{table2.Name}{Environment.NewLine}";
            predictedResult += $"{table1.Name}{Environment.NewLine}";
            predictedResult += $"{table2[2]["name"]}{Environment.NewLine}";
            predictedResult += $"{table2[2]["name"]}{Environment.NewLine}";
            predictedResult += $"{table2[2]["name"]}{Environment.NewLine}";
            predictedResult += $"{table2[2]["name"]}{Environment.NewLine}";
            predictedResult += $"{table2[2]["name"]}{Environment.NewLine}";
            predictedResult += $"{table1.Records.First().Table.Name}";

            string result = parser.Render(model, templateContent);

            // ASSERT
            Assert.NotEqual("", result);
            Assert.Equal(predictedResult, result);
        }

        [Fact]
        public void Parser_indexing_tables_and_fields()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            Model model = new Model();
            Table table1 = DataHelper.AddRandomTable(model, "Table1");
            Table table2 = DataHelper.AddRandomTable(model, "Table2");

            // ACT

            // ASSERT
            Assert.Equal("", parser.Render(model, "{{ model[\"Table3\"] }}"));
            Assert.Equal("", parser.Render(model, "{{ model.Tables[\"Table3\"] }}"));
            Assert.Throws<ParserException>(() => parser.Render(model, "{{ model[999] }}"));
            Assert.Throws<ParserException>(() => parser.Render(model, "{{ model.Tables[999] }}"));
            Assert.Equal("", parser.Render(model, "{{ model.Tables[\"Table1\"][0][\"NON_EXISTING_FIELD\"] }}"));
            Assert.Equal("", parser.Render(model, "{{ model.Tables[\"Table1\"][0].Fields[\"NON_EXISTING_FIELD\"] }}"));
            Assert.Throws<ParserException>(() => parser.Render(model, "{{ model.Tables[\"Table1\"][0][999] }}"));
            Assert.Throws<ParserException>(() => parser.Render(model, "{{ model.Tables[\"Table1\"][0].Fields[999] }}"));
        }

        [Fact]
        public void Parser_with_template_looping_data()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            Model model = new Model();
            Table table1 = DataHelper.AddRandomTable(model, "Table1");
            Table table2 = DataHelper.AddRandomTable(model, "Table2");

            // ACT

            string templateContent =
@"Table count: {{ model.Tables | size }} {% for table in model.Tables -%}
Table {{ table.Name }}:
Field count: {{table.FieldNames | size}} {% for field in table.FieldNames -%}{{- field -}}, {% endfor %}
Record count: {{table.Records | size}} {% for record in table.Records -%}
Field count: {{record.Fields | size}} {% for field in record.Fields -%}{{ field.Value -}}, {% endfor %}
{% endfor -%}
{% endfor -%}";

            string predictedResult = $"Table count: {model.Tables.Count} ";
            foreach (Table table in model.Tables)
            {
                predictedResult += $"Table {table.Name}:{Environment.NewLine}";
                predictedResult += $"Field count: {table.FieldNames.Count} ";
                foreach (string field in table.FieldNames)
                {
                    predictedResult += $"{field}, ";
                }
                predictedResult += $"{Environment.NewLine}Record count: {table.Records.Count} ";
                foreach (Models.Record record in table.Records)
                {
                    predictedResult += $"Field count: {record.Fields.Count} ";
                    foreach (Field field in record.Fields)
                    {
                        if (field.Value is DateTimeOffset)
                        {
                            DateTime dt = ((DateTimeOffset)field.Value).DateTime;
                            predictedResult += $"{dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ")}, ";
                        }
                        else
                        {
                            predictedResult += $"{field.Value}, ";
                        }
                    }
                    predictedResult += $"{Environment.NewLine}";
                }
            }

            string result;
            try
            {
                result = parser.Render(model, templateContent);
            }
            catch (ParserException e)
            {
                result = e.Message;
            }

            // ASSERT
            Assert.NotEqual("", result);
            Assert.Equal(predictedResult, result);
        }

        [Fact]
        public void Parser_check_collection_implementations()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            Model model = new Model();
            Table table1 = DataHelper.AddRandomTable(model, "Table1");
            Table table2 = DataHelper.AddRandomTable(model, "Table2");

            // ACT

            string templateContent =
@"{{ model.Tables | size }}
{{ model.Tables.size }}
{{ model.Tables.first.name }}
{{ model.Tables.last.name }}
{% assign fields = model.Tables[0].Records[0].Fields -%}
{{ fields | size }}
{{ fields.size }}
{{ fields.first.Name }}
{{ fields.last.Name }}
{{ fields.Table.Name }}
{{ fields[0].Name }}";

            string predictedResult = $"{model.Tables.Count}{Environment.NewLine}";
            predictedResult += $"{model.Tables.Count}{Environment.NewLine}";
            predictedResult += $"{model.Tables.First().Name }{Environment.NewLine}";
            predictedResult += $"{model.Tables.Last().Name }{Environment.NewLine}";
            Fields fields = model.Tables.First().Records.First().Fields;
            predictedResult += $"{fields.Count}{Environment.NewLine}";
            predictedResult += $"{fields.Count}{Environment.NewLine}";
            predictedResult += $"{fields.First().Name }{Environment.NewLine}";
            predictedResult += $"{fields.Last().Name }{Environment.NewLine}";
            predictedResult += $"{fields.Table.Name}{Environment.NewLine}";
            predictedResult += $"{fields.First().Name}";

            string result;
            try
            {
                result = parser.Render(model, templateContent);
            }
            catch (ParserException e)
            {
                result = e.Message;
            }

            // ASSERT
            Assert.NotEqual("", result);
            Assert.Equal(predictedResult, result);
        }

        [Fact]
        public void Parser_with_where_filter()
        {
            // ARRANGE
            ParserService parser = new ParserService();

            Model model = new Model();
            Table table1 = DataHelper.AddRandomTable(model, "Table1");
            Table table2 = DataHelper.AddRandomTable(model, "Table2");

            // ACT

            string templateContent =
@"{% assign id = model.Table2[2].Id -%}
Query for id = {{ id }}
{% assign selected = model.Table2.Records | where: ""id"", id -%}
# results={{ selected | size }}
model.Table2.Records | where: ""id"", {{id}} ==> FOUND: id={{ selected.first.id }} name={{ selected.first.name }}";

            string predictedResult = $"Query for id = {model["Table2"][2]["id"]}{Environment.NewLine}# results=1{Environment.NewLine}model.Table2.Records | where: \"id\", {model["Table2"][2]["id"]} ==> FOUND: id={model["Table2"][2]["id"]} name={model["Table2"][2]["name"]}";

            string result;
            try
            {
                result = parser.Render(model, templateContent);
            }
            catch (ParserException e)
            {
                result = e.Message;
            }

            // ASSERT
            Assert.NotEqual("", result);
            Assert.Equal(predictedResult, result);
        }
    }
}
