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