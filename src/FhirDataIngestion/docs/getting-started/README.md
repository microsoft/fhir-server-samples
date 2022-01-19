# Setup and run the FHIR Data Ingestion Tool

You'll find in this document the steps needed to use the FHIR Data Ingestion tool on a developer machine or in a pipeline. The tool supports both Windows and Linux environments. In the next sections, if nothing specific is highlighted, then it means that the instructions are for both environments. Anything specific will be highlighted. For details on how to package the Anonymizer (external) tool in Linux, please read the [following instructions](../technical/anonymizer-dependencies.md).

## Installation

The Claims Data Ingestion tool comes as a package that does include all what's needed. No additional framework or tools should be added. To install the tool automatically, you'll need Azure specific elements for a pipeline installation or if you want to pull the package directly from a script or command line.

### Prerequisites

1. Download and install the latest [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) version.
2. If you're using Linux, make sure you have the appropriate [.NET on Linux](https://docs.microsoft.com/dotnet/core/linux-prerequisites) version.

Note that the az tool is always available in Azure DevOps pipelines. You'll have to install the az tool in other environments.

To check the version of Azure CLI modules and extensions that you currently have, run the following command:

```bash
az --version
```

In the output, you should get `Your CLI is up-to-date.`

If your version is not the last one or you have a doubt, you can run to update to the latest version:

```bash
az upgrade
```

### Login to download the Claims Data Ingestion tool

You'll need to download the tool from [anonymizer-dependencies.md](../technical/anonymizer-dependencies.md) and install it in your local machine.

> **NOTE:** The only difference between the Windows and Linux installation is the name of the package.

This will download a Zip file containing the tool.

## Azure credentials for data uploading

You'll need credentials to Azure to be able to publish the transformed and anonymized data. Note that to create the templates you don't need this. This is needed once you'll want to automate the Claims Data Ingestion tool in a pipeline or in a script with the aim to upload the data.

## Usage

You can run the tool either through `FhirIngestion.Tools.App.exe` for Windows, either `FhirIngestion.Tools.App` for Linux. Make sure for Linux you have the privileges to execute. If not, run a `chmod + x FhirIngestion.Tools.App` to fix this.

The following command lines option are available:

* -c, --configuration: Configuration file as a json file.
* --help: Display this help screen.
* --version: Display version information.

Please make sure to include the mandatory ones.

As an example, this is how can looks like a command line on Windows:

```shell
FhirIngestion.Tools.App.exe -c "c:\temp\config.json"
```

> **NOTE:** If no configuration file is passed, the application will try to load a default file that must be called `config.json`.

## Configuration file

The configuration file is a JSON file and looks like this:

```json
{
  "verboseLogs": true,
  "inputDir": "c:\\temp\\input",
  "outputDir": "c:\\temp\\output",
  "applicationInsightsInstrumentationKey": "<<replace-with-key>>",
  "stages": {
    "converter": {
      "templatesDir": "c:\\temp\\templates",
      "outputDir": ".\\converted"
    },
    "anonymizer": {
      "toolPath": "c:\\temp\\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool",
      "toolConfigPath": "c:\\temp\\Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool\\configuration-sample.json",
      "outputDir": ".\\anonymized"
    },
    "publisher": {
      "aadClientId": "<<replace-with-aadClientId>>",
      "aadClientSecret": "<<replace-with-aadSecret>>",
      "aadResource": "https://<<replace-with-aad-resource-name>>.azurehealthcareapis.com",
      "aadTenantId": "https://login.microsoftonline.com/<<replace-with-tenant-id>>",
      "fhirServerApiUri": "https://<<replace-with-fhir-server-api-uri>>",
      "maxDegreeOfParallelism": 8,
      "maxRetryCount": 3,
      "metricsRefreshInterval": 3,
      "outputResponseBundlesDir": ".\\published"
    }
  }
}
```

### General options

The verbosity `verboseLogs` can be set to true or false and will allow to display detailed transformation information. It is recommended to set it to true for debug purpose, the rest of the time, it is not necessary to do so.

The input directory `inputDir` is mandatory and should contains your parquet or csv files. If the output directory `outputdir` is empty or missing, the input directory will be used.

The Application Insight key `applicationInsightsInstrumentationKey` is not mandatory, if added, it will be used otherwise it will be ignored. You can as well setup an environment variable `APP_INSIGHT_KEY` to setup this value.

### Converter options

The template directory `templatesDir` and the output directory `outputDir` are mandatory. The template directory must contains all your liquid files. The output directory will be used to place the converted files.

### Anonymizer options

All options are mandatory here. The tool path `toolPath`, configuration path `toolConfigPath` and output directory `outputDir`.

### Publisher options

The publishers options are not mandatory. If they are missing, the process will **not** publish the transformed files. Omit this part of the configuration in your development to test the template transformation and the anonymization. Once you are ready, you'll have to set them all properly.

Note that the client ID, secret ID are considered as secrets. See the section above on how you can get them. They can be setup as environment variables and for convenience the tenant ID as well, respectively `PUBLISHER_CLIENT_ID`,
`PUBLISHER_CLIENT_SECRET` and `PUBLISHER_TENANT_ID`.

## FHIR Anonimizer dependencies

The FhirIngestion.Tools has a dependency on an external tool used for the anonymization called `Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool`. While packaged with the FhirIngestion.Tools, it can be updated. You will find a detailed documentation on how to package and test the tool in [this document](../technical/anonymizer-dependencies.md).
