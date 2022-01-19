using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FhirIngestion.Tools.Converter.Models;
using FhirIngestion.Tools.Converter.Tests.Helpers;
using Bogus;
using Xunit;

namespace FhirIngestion.Tools.Converter.Tests
{
    [ExcludeFromCodeCoverage]
    public class ModelTests
    {
        [Fact]
        public void Create_empty_model()
        {
            // ARRANGE
            Faker faker = new Faker();
            Model model = new Model();

            // ASSERT
            Assert.NotNull(model);
            Assert.Empty(model.Tables);
        }

        [Fact]
        public void Create_tables()
        {
            // ARRANGE
            string tablename = "Table1";
            Table tableNoFields = new Table(tablename);
            Table tableWithFields = new Table(tablename, DataHelper.GetFieldNames());

            // ACT
            Tables tables = new Tables();
            tables.Add(tablename, DataHelper.GetFieldNames());

            // ASSERT
            // detect duplicate tablename
            Assert.Throws<ApplicationException>(() => { tables.Add(tablename, DataHelper.GetFieldNames()); });

            // accessing a non existing table should return null for Liquid
            Assert.Null(tables["NON_EXISTING_TABLE"]);
        }

        [Fact]
        public void Create_table()
        {
            // ACT
            string tablename = "Table1";
            Table tableNoFields = new Table(tablename);
            Table tableWithFields = new Table(tablename, DataHelper.GetFieldNames());

            // ASSERT
            Assert.NotNull(tableNoFields);
            Assert.Equal(tablename, tableNoFields.Name);
            Assert.Empty(tableNoFields.FieldNames);
            Assert.Empty(tableNoFields.Records);

            Assert.NotNull(tableWithFields);
            Assert.Equal(tablename, tableWithFields.Name);
            Assert.NotEmpty(tableWithFields.FieldNames);
            Assert.Empty(tableWithFields.Records);

            // create with empty name fails
            Assert.Throws<ArgumentException>(() => new Table(null));
            Assert.Throws<ArgumentException>(() => new Table(""));
            Assert.Throws<ArgumentException>(() => new Table("     "));
        }

        [Fact]
        public void Create_records()
        {
            // ACT
            string tablename = "Table1";
            Table tableNoFields = new Table(tablename);
            Table tableWithFields = new Table(tablename, DataHelper.GetFieldNames());

            Records recordsNoFields = new Records(tableNoFields);
            Records recordsWithFields = new Records(tableWithFields);

            // ASSERT
            Assert.NotNull(recordsNoFields);
            Assert.Equal(tableNoFields, recordsNoFields.Table);
            Assert.Empty(recordsNoFields);

            Assert.NotNull(recordsWithFields);
            Assert.Equal(tableWithFields, recordsWithFields.Table);
            Assert.Empty(recordsNoFields);

            // create with null fails
            Assert.Throws<ArgumentNullException>(() => new Records(null));
            // create with no fields, where columns are defined
            Assert.Throws<ArgumentException>(() => recordsWithFields.AddValues());
        }

        [Fact]
        public void Create_record()
        {
            // ACT
            string tablename = "Table1";
            Table tableNoFields = new Table(tablename);
            Table tableWithFields = new Table(tablename, DataHelper.GetFieldNames());

            int id = 1000;
            Models.Record recordNoFields = new Models.Record(tableNoFields);
            object[] values = DataHelper.GetRandomValues(id);
            Models.Record recordWithFields = new Models.Record(tableWithFields, values);

            // ASSERT
            Assert.NotNull(recordNoFields);
            Assert.Equal(tableNoFields, recordNoFields.Table);
            Assert.Empty(recordNoFields.Fields);

            Assert.NotNull(recordWithFields);
            Assert.Equal(tableWithFields, recordWithFields.Table);
            Assert.NotEmpty(recordWithFields.Fields);
            Assert.Equal(id, recordWithFields[0]);
            Assert.Equal(id, recordWithFields["id"]);
            Assert.Equal(id, recordWithFields.Fields[0].Value);
            Assert.Equal(id, recordWithFields.Fields["id"]);
            Assert.Equal(values[1], recordWithFields["name"]);
            Assert.Equal(values[2], recordWithFields["number"]);
            Assert.Equal(values[3], recordWithFields["date"]);
            Assert.Equal(recordWithFields.Fields.Count, recordWithFields.GetValues().Length);

            // create with null fails
            Assert.Throws<ArgumentNullException>(() => new Models.Record(null));
            Assert.Throws<ArgumentException>(() => new Models.Record(tableWithFields, new List<object>()));
            // creating a record without the same amount of fields throws
            Assert.Throws<ArgumentException>(() => new Models.Record(tableWithFields, "JUST_ONE_FIELD"));
        }

        [Fact]
        public void Create_fields()
        {
            // ACT
            string tablename = "Table1";
            Table tableNoFields = new Table(tablename);
            Table tableWithFields = new Table(tablename, DataHelper.GetFieldNames());

            Fields fieldsEmpty = new Fields(tableNoFields);
            Fields fieldsNotEmpty = new Fields(tableWithFields);
            string testValue = "My Test Value";
            fieldsNotEmpty.Add(new Field(tableWithFields, tableWithFields.FieldNames[0]) { Value = testValue });

            // ASSERT
            Assert.NotNull(fieldsEmpty);
            Assert.Equal(tableNoFields, fieldsEmpty.Table);
            Assert.Empty(fieldsEmpty);

            Assert.NotNull(fieldsNotEmpty);
            Assert.Equal(tableWithFields, fieldsNotEmpty.Table);
            Assert.NotEmpty(fieldsNotEmpty);

            Assert.Equal(testValue, fieldsNotEmpty["id"]);
            // accessing a non existing field by name should return null for Liquid
            Assert.Null(fieldsEmpty["NON_EXISTING_TABLE"]);
            Assert.Null(fieldsNotEmpty["NON_EXISTING_TABLE"]);

            // creating fields with null fails
            Assert.Throws<ArgumentNullException>(() => new Fields(null));
        }

        [Fact]
        public void Create_field()
        {
            // ACT
            string tablename = "Table1";
            Table table = new Table(tablename);

            string fieldname = "Field1";
            Field field = new Field(table, fieldname);

            // ASSERT
            Assert.NotNull(field);
            Assert.Equal(table, field.Table);
            Assert.Equal(fieldname, field.Name);
            Assert.Null(field.Value);

            // creating fields with null fails
            Assert.Throws<ArgumentNullException>(() => new Field(null, fieldname));
            Assert.Throws<ArgumentException>(() => new Field(table, null));
            Assert.Throws<ArgumentException>(() => new Field(table, ""));
            Assert.Throws<ArgumentException>(() => new Field(table, "      "));
        }

        [Fact]
        public void Create_basic_model_success()
        {
            // ARRANGE
            Model model = new Model();

            // ACT
            int maxRecords = 10;
            string table1name = "Table1";
            Table table1 = DataHelper.AddRandomTable(model, table1name, maxRecords);
            string name1 = (string)table1.Records[0].Fields[1].Value;
            string table2name = "Table2";
            Table table2 = DataHelper.AddRandomTable(model, table2name, maxRecords);

            // ASSERT
            Assert.Equal(2, model.Tables.Count);

            Assert.NotNull(model[table1name]);
            Assert.NotNull(model.Tables[0]);
            Assert.NotNull(model.Tables[table1name]);

            Assert.NotNull(model[table2name]);
            Assert.NotNull(model.Tables[1]);
            Assert.NotNull(model.Tables[table2name]);

            Assert.Equal(table1name, model[table1name].Name);
            Assert.Equal(table1name, model.Tables[0].Name);
            Assert.Equal(table1name, model.Tables[table1name].Name);

            Assert.Equal(table2name, model[table2name].Name);
            Assert.Equal(table2name, model.Tables[1].Name);
            Assert.Equal(table2name, model.Tables[table2name].Name);

            // get first records
            Assert.NotNull(model[table1name][0]);
            Assert.NotNull(model[table2name][0]);

            // get name in first record
            Assert.Equal(name1, model[table1name][0]["name"]);

            Assert.Null(model["NON_EXISTING_TABLE"]);
            Assert.Null(model[table1name][0]["NON_EXISTING_FIELD"]);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var t = model[table1name][99]; });
        }
    }
}
