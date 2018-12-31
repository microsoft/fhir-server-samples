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

# Get current AzureAd context
try {
    $tenantInfo = Get-AzureADCurrentSessionInfo -ErrorAction Stop
} 
catch {
    throw "Please log in to Azure AD with Connect-AzureAD cmdlet before proceeding"
}

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
$serviceClientId  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-id").SecretValueText
$serviceClientSecret  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-secret").SecretValueText

$env:FhirServerUrl = $fhirServerUrl
$env:DashboardUrl = $dashboardUrl
$env:DashboardUserUpn = $dashboardUserUpn
$env:DashboardUserPassword = $dashboardUserPassword
$env:ServiceClientId = $serviceClientId
$env:ServiceClientSecret = $serviceClientSecret

@{
    dashboardUrl              = $dashboardUrl
    fhirServerUrl             = $fhirServerUrl
    dashboardUserUpn          = $dashboardUserUpn
    dashboardUserPassword     = $dashboardUserPassword
    serviceClientId           = $serviceClientId
    serviceClientSecret       = $serviceClientSecret
}
