> ⚠️ Thank you for your interest in our repository. **As of May 13, 2022, this repository has been archived, and is no longer maintained or updated.**


# FHIR Server Samples

This respository contains example applications and scenarios that show use of the [FHIR Server for Azure](https://github.com/Microsoft/fhir-server) and the [Azure API for FHIR](https://docs.microsoft.com/azure/healthcare-apis).

The scenario is meant to illustrate how to connect a web application to the FHIR API. The scenario also illustrates features such as the SMART on FHIR Active Directory Proxy. It can be deployed using the Azure API for FHIR PaaS server:

<center><img src="images//fhir-server-samples-paas.png" width="512"></center>

Or the open source FHIR Server for Azure:

<center><img src="images//fhir-server-samples-oss.png" width="512"></center>

In both cases a storage account will be deploy and in this storage account there is a BLOB container called `fhirimport`, patient bundles generated with [Synthea](https://github.com/synthetichealth/synthea) can dumped in this storage container and they will be ingested into the FHIR server. The bulk ingestion is performed by an Azure Function.

The environments can also optionally be configured to support [`$export`](https://hl7.org/Fhir/uv/bulkdata/export/index.html). To enable `$export`, add the `-EnableExport $true` parameter to the script below. The `$export` operation will produce a new line delimited json (ndjson) for each resource type. These ndjson files are easily consumed with something like [Databricks](https://azure.microsoft.com/en-us/services/databricks/) (Apache-Spark). Please see the [analytics](analytics/) folder for some details and example queries. Note that the Databricks environment is not deployed automatically with the sandbox and must be set up separately.

> Note: To enable `$export` you must have subscription rights that allow you to set data plane access roles for storage accounts, e.g. you must be a subscription owner.

# Prerequisites

Before deploying the samples scenario, make sure you have `Az` and `AzureAd` powershell modules installed:

```PowerShell
Install-Module Az
Install-Module AzureAd
```

The new `Az` module requires **PowerShell version 5.1 or above** installed on your computer. So if you have PowerShell version below 5.1, you need to update it. To check your PowerShell version, you can run:
```PowerShell
$PSVersionTable.PSVersion
```
Currently, there is a **bug with PowerShell Az Module version 4.6.1** confirmed with Azure ARM team. For now, **please avoid using version 4.6.1**. Version 4.5 and versions 4.7.0 or above should work fine. To check your Az module version, you can run:
```PowerShell
Get-InstalledModule -Name Az
```

# Deployment

To deploy the sample scenario, first clone this git repo and find the deployment scripts folder:

```PowerShell
git clone https://github.com/Microsoft/fhir-server-samples
cd fhir-server-samples/deploy/scripts
```

Log into your Azure subscription:

```PowerShell
Login-AzAccount
```

Connect to Azure AD with:

```PowerShell
Connect-AzureAd -TenantDomain <AAD TenantDomain>
```

**NOTE** The connection to Azure AD can be made using a different tenant domain than the one tied to your Azure subscription. If you don't have privileges to create app registrations, users, etc. in your Azure AD tenant, you can [create a new one](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant), which will just be used for demo identities, etc.

Then, deploy the scenario with the Open Source FHIR Server for Azure:

```PowerShell
.\Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME> -UsePaaS $false
```

or the managed Azure API for FHIR:

```PowerShell
.\Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME> -UsePaaS $true
```

and to enable `$export`:

```PowerShell
.\Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME> -UsePaaS $true -EnableExport $true
```

To delete the senario:

```PowerShell
.\Delete-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME>
```

**NOTE** If you are using PowerShell Core on other platforms (macOS or Linux), please make sure to specify password in the command. You can do this by:

```PowerShell
./Create-FhirServerSamplesEnvironment.ps1 -EnvironmentName <ENVIRONMENTNAME> -UsePaaS <TRUE/FALSE> -AdminPassword $(ConvertTo-SecureString -AsPlainText -Force "<YOURPASSWORD>")
```

If the deployment is successful, you would see information like below being written on your terminal or CloudShell as the scripts run:
```PowerShell
Current context is user: xxxx
FhirServer PS module is loaded
Current context is user: xxxx
Adding permission to keyvault for xxxx
Ensuring API application exists
Checking if UserPrincipalName exists
User not, will create.

DeploymentName          : xxxx
ResourceGroupName       : xxxx
ProvisioningState       : Succeeded
Timestamp               : 11/24/2020 10:30:18 PM
Mode                    : Incremental
TemplateLink            : 
                          Uri            : https://raw.githubusercontent.com/Microsoft/fhir-server-samples/master/deploy/templates/a
                          zuredeploy-sandbox.json
                          ContentVersion : 1.0.0.0
                          
Parameters              : 
                          Name                             Type                       Value     
                          ===============================  =========================  ==========
                          environmentName                  String                     xxxx
                          appServicePlanSku                String                     xxxx       
                          aadAuthority                     String                     
                          xxxx
                          aadFhirServerAudience            String                               
                          aadDashboardClientId             String                     xxxx
                          aadDashboardClientSecret         String                     xxxx
                          aadServiceClientId               String                     xxxx
                          aadServiceClientSecret           String                     xxxx
                          smartAppClientId                 String                     xxxx
                          fhirServerTemplateUrl            String                     
                          https://raw.githubusercontent.com/microsoft/fhir-server/master/samples/templates/default-azuredeploy.json
                          sqlAdminPassword                 SecureString                         
                          fhirDashboardJSTemplateUrl       String                     https://raw.githubusercontent.com/Microsoft/fh
                          ir-server-samples/master/deploy/templates/azuredeploy-fhirdashboard-js.json
                          fhirApiLocation                  String                     westus2   
                          fhirVersion                      String                     R4        
                          fhirImporterTemplateUrl          String                     https://raw.githubusercontent.com/Microsoft/fh
                          ir-server-samples/master/deploy/templates/azuredeploy-importer.json
                          smartAppTemplateUrl              String                               
                          fhirDashboardRepositoryUrl       String                     
                          https://github.com/Microsoft/fhir-server-samples
                          fhirDashboardRepositoryBranch    String                     master    
                          deployDashboardSourceCode        Bool                       True      
                          usePaaS                          Bool                       True      
                          accessPolicies                   Array                      [
                            {
                              "objectId": "xxxx"
                            },
                            {
                              "objectId": "xxxx"
                            },
                            {
                              "objectId": "xxxx"
                            }
                          ]
                          solutionType                     String                     FhirServerSamples
                          enableExport                     Bool                       False     
                          
Outputs                 : 
DeploymentDebugLogLevel : 

Warming up site...

Key   : fhirServerUrl
Value : https://xxxx.azurehealthcareapis.com
Name  : fhirServerUrl


Key   : dashboardUserUpn
Value : xxxx
Name  : dashboardUserUpn


Key   : dashboardUserPassword
Value : xxxx
Name  : dashboardUserPassword

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
