#!/usr/bin/env pwsh
<#
.SYNOPSIS
Imports data into an Azure FHIR server.
.DESCRIPTION
#>
param (
  [Parameter(Mandatory=$true)]
  [string]
  [ValidatePattern('^https://[^.]+\.azurehealthcareapis\.com$')]
  $FhirServerUrl,

  [Parameter(Mandatory=$true)]
  [string]
  [ValidateScript({ if (!(Test-Path $_ -PathType Container)) { throw 'Must be a directory' } else { return $true } })]
  $BundlesDirectory
)

if (!(Get-Command az -ErrorAction SilentlyContinue))
{
  throw 'Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli'
}

try
{
  $user = az account show --query 'user.name' --output tsv
}
catch
{
  throw 'Please log in to Azure CLI with the az login command before proceeding'
}

Write-Output "Getting access token for $FhirServerUrl"

$fhirResourceName = ($FhirServerUrl | Select-String -Pattern '^https://([^.]+)\.azurehealthcareapis\.com$').Matches.Groups[1].Value

$fhirResourceId = az resource list `
  --namespace Microsoft.HealthcareApis `
  --resource-type services `
  --name $fhirResourceName `
  --query '[0].id' `
  --output tsv

$roleAssignmentResourceId = az role assignment create `
  --assignee $user `
  --scope $fhirResourceId `
  --role 'FHIR Data Contributor' `
  --query 'id' `
  --output tsv

try
{
  $accessToken = az account get-access-token `
    --resource $FhirServerUrl `
    --query 'accessToken' `
    --output tsv

  $jsonFiles = Get-ChildItem -Path $BundlesDirectory -Filter '*.json'

  foreach ($jsonFile in $jsonFiles)
  {
    Write-Output "Processing file $jsonFile"
    $bundle = Get-Content $jsonFile.FullName | ConvertFrom-Json

    foreach ($entry in $bundle.entry)
    {
      $headers = @{}
      $headers.Add('Authorization', "Bearer $accessToken")
      $headers.Add('Content-Type', 'application/fhir+json')

      $response = Invoke-WebRequest `
        -Uri $($FhirServerUrl + '/' + $entry.request.url) `
        -Method $entry.request.method `
        -Body $(ConvertTo-Json $entry.resource -Depth 10) `
        -Headers $headers `
        -UseBasicParsing

      $resourceUri = $response.Headers['Location']
      Write-Output "Imported resource $resourceUri"
    }
  }
}
finally
{
  Write-Output 'Cleaning up'
  az role assignment delete `
    --id $roleAssignmentResourceId `
    --yes
}
