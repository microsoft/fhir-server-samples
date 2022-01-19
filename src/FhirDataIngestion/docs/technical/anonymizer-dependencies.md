# Anonymization tool dependencies

## Introduction

The `FhirIngestion.Tools.App` is a cross-platform (cli) tool that depends on the (external) [Tools for Health Data Anonymization](https://github.com/microsoft/Tools-for-Health-Data-Anonymization) open-source project.

This particular tool is used in the 2nd stage, to anonymize FHIR JSON data. This is achieved by executing the external tool using [SimpleExec](https://github.com/adamralph/simple-exec). The actual implementation can be found in [AnonymizeProcess.cs](../../src/FhirIngestion.tools.Anonymizer/AnonymizeProcess.cs).

Currently, the [Tools for Health Data Anonymization](https://github.com/microsoft/Tools-for-Health-Data-Anonymization) is only [released for Windows](https://github.com/microsoft/Tools-for-Health-Data-Anonymization/releases/tag/v3.0.0). Therefore, extra steps are required in to support Linux.

The following document describes the steps to clone, compile the [Tools for Health Data Anonymization](https://github.com/microsoft/Tools-for-Health-Data-Anonymization) for Linux (x64). This documentation will give you the steps to test manually all this in a context of a new version coming. The end to end tests will validate that all is working anyway.

## Pre-requisites

These are the following hardware and software requirements:

- A **Linux (x64)** capable machine, to compile the source-code ([WSL2](https://docs.microsoft.com/en-us/windows/wsl/install) also works).
- [Git](https://git-scm.com/downloads) SCM.
- [.NET Core SDK 3.1.x](https://dotnet.microsoft.com/download/dotnet/3.1)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt) Additional install instruction [here](../README.md#anchor-installation).

## Clone, Build and Publishing steps

Github CI workflow helps you to automate below steps.
You can check all the steps in [.github/workflow/ci.yaml](../../.github/workflows/ci.yaml) file.

### Step 1

Clone the **Tools for Health Data Anonymization** repository to a **Linux (x64) capable machine**

```bash
git clone https://github.com/microsoft/Tools-for-Health-Data-Anonymization.git
```

### Step 2

Browse to the Tools-for-Health-Data-Anonymization/FHIR directory and build the **Fhir.Anonymizer.sln solution**.

```bash
cd Tools-for-Health-Data-Anonymization/FHIR
dotnet build Fhir.Anonymizer.sln -c Release
```

A successful build output should be similar to this:

```bash
Microsoft (R) Build Engine version 16.11.1+3e40a09f8 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  Microsoft.Health.Fhir.Anonymizer.Stu3.Core -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.Core/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.Core.dll
  Microsoft.Health.Fhir.Anonymizer.R4.Core -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.Core/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.Core.dll
  Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.CommandLineTool.dll
  Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline.dll
  Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.dll
  Microsoft.Health.Fhir.Anonymizer.Stu3.Core.UnitTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.Core.UnitTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.Core.UnitTests.dll
  Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline.dll
  Microsoft.Health.Fhir.Anonymizer.R4.Core.UnitTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.Core.UnitTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.Core.UnitTests.dll
  Microsoft.Health.Fhir.Anonymizer.Stu3.FunctionalTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.FunctionalTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.FunctionalTests.dll
  Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline.UnitTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline.UnitTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.AzureDataFactoryPipeline.UnitTests.dll
  Microsoft.Health.Fhir.Anonymizer.R4.FunctionalTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.R4.FunctionalTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.FunctionalTests.dll
  Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline.UnitTests -> /mnt/c/Projects/Tools-for-Health-Data-Anonymization/FHIR/src/Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline.UnitTests/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.Stu3.AzureDataFactoryPipeline.UnitTests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:09.33
```

1. Next, you can check the compiled binaries here:

```bash
ls src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/netcoreapp3.1/
```

1. You can also validate that it's working by executing the (freshly) compiled tool:

```bash
./src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool

Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool 1.0.0
Copyright (C) 2021 Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool

ERROR(S):
  Required option 'i, inputFolder' is missing.
  Required option 'o, outputFolder' is missing.

  -i, --inputFolder     Required. Folder to locate input resource files.

  -o, --outputFolder    Required. Folder to save anonymized resource files.

  -c, --configFile      (Default: configuration-sample.json) Anonymizer configuration file path.

  -b, --bulkData        (Default: false) Resource file is in bulk data format (.ndjson).

  -s, --skip            (Default: false) Skip existed files in target folder.

  -r, --recursive       (Default: false) Process resource files in input folder recursively.

  -v, --verbose         (Default: false) Provide additional details in processing.

  --validateInput       (Default: false) Validate input resources. Details can be found in verbose log.

  --validateOutput      (Default: false) Validate anonymized resources. Details can be found in verbose log.

  --help                Display this help screen.

  --version             Display version information.
```

Troubleshooting tip:

```bash
# In case you can not execute the command, you can try to change the file permissions to allow for execution and retry.
chmod +x ./src/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool/bin/Release/netcoreapp3.1/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool

```

## Additional remarks

When the time comes to leverage a new release of the tool, you can follow the same steps described above. In the case for Windows, all it needs to be done is to download the [latest Windows release from Github.com](https://github.com/microsoft/Tools-for-Health-Data-Anonymization/releases/download/v3.0.0/Microsoft.Health.Fhir.Anonymizer.R4.CommandLineTool.zip), unzip and publish it as a new version.

The package for this tool is not published a single packaged file. Meaning, all the dependencies are in separated files. This will allow a better debug experience if needed.
