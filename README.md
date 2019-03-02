# FHIR Server Samples

This respository contains example applications and scenarios that show use of the [FHIR Server for Azure](https://github.com/Microsoft/fhir-server) and the [Azure API for FHIR](https://docs.microsoft.com/azure/healthcare-apis).

The scenario is meant to illustrate how to connect a web application to the FHIR API. When deployed with the Open Source FHIR server novel features, such as the SMART on FHIR Active Directory Proxy, are demonstrated:

<img src="images//fhir-server-samples-oss.png" width="320">

The scenario using the Azure API for FHIR PaaS server will deploy without the SMART on FHIR apps:

<img src="images//fhir-server-samples-paas.png" width="320">

In both cases a storage account will be deploy and in this storage account there is a BLOB container called `fhirimport`, patient bundles generated with [Synthea](https://github.com/synthetichealth/synthea) can dumped in this storage container and they will be ingested into the FHIR server. The bulk ingestion is performed by an Azure Function.

# Deployment

To deploy the sample scenario, first clone this git repo and find the deployment scripts folder:

```PowerShell
git clone https://github.com/Microsoft/fhir-server-samples
cd fhir-server-samples/deploy/scripts
```

Log into your Azure subscription:

```PowerShell
Login-AzureRmAccount -TenantId <AAD Tenant>
```

Connect to Azure AD with:

```PowerShell
Connect-AzureAd -TenantDomain <AAD TenantDomain>
```

*If you are deploying the scenario using the managed Azure API for FHIR, the AAD tenants should be the same when connecting to `AzureAD` and `AzureRm`.*

Then deploy the scenario with the Open Source FHIR Server for Azure:

```PowerShell
.\Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME>
```

or the managed Azure API for FHIR:

```PowerShell
.\Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME> -UsePaaS $true
```

To delete the senario:

```PowerShell
.\Delete-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME>
```

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
