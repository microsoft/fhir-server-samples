namespace FhirIngestion.Tools.Converter.Tests.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using FhirIngestion.Tools.Converter.Models;
    using Bogus;

    [ExcludeFromCodeCoverage]
    public static class DataHelper
    {
        public static string[] GetFieldNames()
        {
            return new string[] { "Id", "Name", "Number", "Date" };
        }

        public static Table AddRandomTable(Model model, string tableName, int max = 10)
        {
            Faker faker = new Faker();
            Table table = model.Tables.Add(tableName, GetFieldNames());

            int id = 1000;
            for (int i = 0; i < max; i++)
            {
                object[] values = GetRandomValues(id++);
                table.Records.AddValues(values);
            }
            return table;
        }

        public static object[] GetRandomValues(int id)
        {
            Faker faker = new Faker();
            return new object[] { id, faker.Name.FullName(), faker.Random.Number(50000), (DateTimeOffset)faker.Date.Past(1) };
        }
    }
}
