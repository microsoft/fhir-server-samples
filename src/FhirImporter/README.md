# Azure FHIR Importer Function

The Azure function app will monitor a `fhirimport` container in the attached storage account and ingest patient bundles into the FHIR service.

The function can be deployed with the [azuredeploy-importer.json](../../deploy/templates/azuredeploy-importer.json) template, whcih will use an Azure Function App (App Service).

It can also be deployed as a container using Azure Container Instances. For this approach, first build the Docker image:

```
docker build -t reponame/fhirimporter .
docker push reponame/fhirimporter
```

When deploying, the following environment variables should be set:

```
APPINSIGHTS_INSTRUMENTATIONKEY=<KEY>
Audience=<e.g. https://azurehealthcareapis.com>
Authority=<e.g. https://login.microsoftonline.com/TENANT-ID>
AzureWebJobsDashboard=<STORAGE ACCOUNT CONNECTION STRING>
AzureWebJobsStorage=<STORAGE ACCOUNT CONNECTION STRING>
ClientId=<CLIENT ID (service client)>
ClientSecret=<CLIENT SECRET>
FhirServerUrl=<e.g. https://myaccount.azurehealthcareapis.com>
MaxDegreeOfParallelism=<default 16>	
```

For a single command line deployment of the container instance:

```
az container create --resource-group <RESOURCE GROUP NAME> --image reponame/fhirimporter --name fhirimporter1 --cpu 2 --memory 2 --environment-variables APPINSIGHTS_INSTRUMENTATIONKEY='<KEY>' Audience='<e.g. https://azurehealthcareapis.com>' Authority='<<e.g. https://login.microsoftonline.com/TENANT-ID>' AzureWebJobsDashboard='<STORAGE ACCOUNT CONNECTION STRING>' AzureWebJobsStorage='<STORAGE ACCOUNT CONNECTION STRING>' ClientId='<CLIENT ID>' ClientSecret='<CLIENT SECRET>' FhirServerUrl='<e.g. https://myaccount.azurehealthcareapis.com>' MaxDegreeOfParallelism=16
```

There is also a template [azuredeploy-aci-importer.json](../../deploy/templates/azuredeploy-aci-importer.json), which will deploy a storage account, application insights and the function in an Azure Container Instance. Deploy that template with:

```
 az group deployment create --resource-group <RESOURCE-GROUP-NAME> --template-file .\azuredeploy-aci-importer.json --parameters @aci-template.parameters.json
```

where `aci-template.parameters.json` is a parameter file with the following contents:

```json
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
       "appName": {
           "value": "my-aci-importer-app"
       },
       "aadAuthority": {
           "value": "https://login.microsoftonline.com/<TENANT-ID>"
       },
       "aadAudience": {
        "value": "https://azurehealthcareapis.com"
       },
       "aadServiceClientId": {
        "value": "<SERVICE CLIENT ID>"
       },
       "aadServiceClientSecret": {
        "value": "<SERVICE-CLIENT-SECRET>"
       },
       "fhirServerUrl": {
        "value": "https://<myaccountname>.azurehealthcareapis.com"
       }
    }
}
```

