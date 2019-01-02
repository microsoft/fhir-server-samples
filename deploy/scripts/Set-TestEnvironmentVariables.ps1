<#
.SYNOPSIS
Sets environment variables for E2E integration tests
.DESCRIPTION
#>
param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EnvironmentName
)

Set-StrictMode -Version Latest

# Get current AzureRm context
try {
    $azureRmContext = Get-AzureRmContext
} 
catch {
    throw "Please log in to Azure RM with Login-AzureRmAccount cmdlet before proceeding"
}

$dashboardUrl = "https://${EnvironmentName}dash.azurewebsites.net"
$fhirServerUrl = "https://${EnvironmentName}srvr.azurewebsites.net"
$dashboardUserUpn  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-upn").SecretValueText
$dashboardUserPassword  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-password").SecretValueText
$confidentialClientId  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-id").SecretValueText
$confidentialClientSecret  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-secret").SecretValueText
$serviceClientId  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-id").SecretValueText
$serviceClientSecret  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-secret").SecretValueText

$storageAccountName = ("${EnvironmentName}dashsa").Replace('-','');
$storageAccountKey = (Get-AzureRmStorageAccountKey -Name $storageAccountName -ResourceGroupName $EnvironmentName)[0].Value
$storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey}"

$env:StorageAccountConnectionString = $storageAccountConnectionString
$env:FhirServerUrl = $fhirServerUrl
$env:DashboardUrl = $dashboardUrl
$env:DashboardUserUpn = $dashboardUserUpn
$env:DashboardUserPassword = $dashboardUserPassword
$env:ConfidentialClientId = $confidentialClientId
$env:ConfidentialClientSecret = $confidentialClientSecret
$env:ServiceClientId = $serviceClientId
$env:ServiceClientSecret = $serviceClientSecret

@{
    dashboardUrl                   = $dashboardUrl
    fhirServerUrl                  = $fhirServerUrl
    dashboardUserUpn               = $dashboardUserUpn
    dashboardUserPassword          = $dashboardUserPassword
    confidentialClientId           = $serviceClientId
    confidentialClientSecret       = $serviceClientSecret
    serviceClientId                = $serviceClientId
    serviceClientSecret            = $serviceClientSecret
    storageAccountConnectionString = $storageAccountConnectionString
}
