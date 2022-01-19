# Adding new data format to Data Converter

`FhirIngestion.Tools.App` project supports two different data formats: `Parquet` and `CSV` file formats. In the data converter process, data is loaded in `FhirIngestion.Tools.Converter` project. It is very flexible and easy to introduce new data formats and reflect the format changes in this module.

## How to Introduce a new file format

In order to introduce a new file format, you need to:

1. Place data files in a folder.
2. Implement the new file format in the `FhirIngestion.Tools.Converter` project.
3. Add unit tests for your new file format.
4. Reflect data input path in config file.

## Reflection in C# code

1. Add a data service class in the `"Services"` folder:

    As an example `CSVService.cs` file is added to `FhirIngestion.Tools.Converter` project under `"Services"`. Make sure to implement a `FileNotifierDelegate` based event to let the caller know which file you're processing. This is useful for logging and verbose mode.

    Also implement the `ImportFiles` method to import the data into the model.

    > **NOTE:** Currently only tabular data is supported. If you want to support other types like JSON or XML, you must decide on a strategy how to make that accessible.
    > One idea could be to add a collection of JSON-structures to the Model, where each structure can be referenced by a unique name.

2. Add Telemetry for new data type in the service class:

    Another important point is tracing whole data conversion process. This is done by adding a `ApplicationInfoTelemetry.TrackMetric`. In the implementation three major metrics are tracked, in Application Insights these are differentiated by data type, once you add a new data type you need to add a new metric. As an example CSV telemetry points are added:

    Number of imported lines per file:

    ```csharp
    ApplicationInfoTelemetry.TrackMetric($"CSVServiceImportLines-{file}", lines.Length);
    ```

    Number of imported files into model:

    ```csharp
    ApplicationInfoTelemetry.TrackMetric("CSVServiceImportFiles", files.Length);
    ```

    (Optional, if `Export` is implemented) successfully exported number of lines:

    ```csharp
    ApplicationInfoTelemetry.TrackMetric("CSVServiceExportLines", lines.Count);
    ```

3. Call the data service in `ConverterProcess`:

    As an example, see below `CSVService` implementation to read all CSV files and import into model.

    ```csharp
    // read all CSV files next
    CSVService csv = new CSVService();

    if (Options.VerboseLogs)
    {
        csv.ImportingFile += (path) =>
        {
            MessageHelper.Verbose($"Importing {Path.GetFileName(path)}");
        };
        MessageHelper.Verbose($"Importing CSV files from {Options.InputDir}.");
    }

    csv.ImportFiles(model, Options.InputDir);
    ```

4. Load and map to table structure:

    With the standard tabular structure, each imported file is accessible by it's name in the `Model.Tables` collection. The name is unique in that collection, so if you have different input types with the same name, it will cause an exception 'table already exists'.

## Reflection in Unit Tests

First of all prepare valid and invalid test files in the `FhirIngestion.Tools.Converter.Tests` project. You can add test to the `ServiceTests` class or create a separate test cases for valid and invalid scenarios.

## Reflection in the config file

Once new data format is introduced, you could add a data input path to the config file if you want it to be in a different place than the other data files. Currently there is only one input folder for Parquet files and CSV files.

```json
...
  "inputDir": "c:\\temp\\new_data_input_folder",
...
```

## Reflection in Templates

The data model is loading tables from selected data files. Now the Liquid templates can access the data through the `Model` class. If there is any change in a column name, this should also be changed in the Liquid templates.

## Conclusion

`FhirIngestion.Tools.Converter` is very flexible and easy to introduce new data formats. Muliple data input formats can be used in the same application. Make sure your data is correctly loaded into the `Model` and your data structure is correctly mapped through the Liquid templates.

There is no need to change `Anonymizer` and `Publisher` steps, output of the Liquid templates will be same for all data types and FHIR files will be handled by the `FhirIngestion.Tools.Publisher` project.
