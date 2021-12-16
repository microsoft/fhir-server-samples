<#
.SYNOPSIS
Deletes application registrations and user profiles from an AAD tenant
.DESCRIPTION
#>
param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EnvironmentName,

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentLocation = "westus",

    [Parameter(Mandatory = $false )]
    [String]$WebAppSuffix = "azurewebsites.net",

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = $EnvironmentName,

    [parameter(Mandatory = $false)]
    [string]$KeyVaultName = "$EnvironmentName-ts"
)

Set-StrictMode -Version Latest

# Get current AzureAd context
try {
    $tenantInfo = Get-AzureADCurrentSessionInfo -ErrorAction Stop
} 
catch {
    throw "Please log in to Azure AD with Connect-AzureAD cmdlet before proceeding"
}

# Ensure that we have the FhirServer PS Module loaded
if (Get-Module -Name FhirServer) {
    Write-Host "FhirServer PS module is loaded"
} else {
    Write-Host "Fetching FHIR Server repo to get access to FhirServer PS module."
    $fhirServerVersion = 'main'
    if (!(Test-Path -Path ".\fhir-server-$fhirServerVersion")) {
        (New-Object System.Net.WebClient).DownloadFile("https://github.com/Microsoft/fhir-server/archive/$fhirServerVersion.zip", "$PWD/fhir-server-$fhirServerVersion.zip")
        Expand-Archive -Path ".\fhir-server-$fhirServerVersion.zip" -DestinationPath "$PWD"
        Remove-Item ".\fhir-server-$fhirServerVersion.zip"
    }
    Import-Module ".\fhir-server-$fhirServerVersion\samples\scripts\PowerShell\FhirServer\FhirServer.psd1"
}

$fhirServiceName = "${EnvironmentName}srvr"
$fhirServiceUrl = "https://${fhirServiceName}.${WebAppSuffix}"
$fhirAADUrl = "https://${fhirServiceName}.$($tenantInfo.TenantDomain)"  
$PaasUrl = "https://${EnvironmentName}.azurehealthcareapis.com"

$application = Get-AzureAdApplication -Filter "identifierUris/any(uri:uri eq '$PaasUrl')"

if ($application) {
    Remove-FhirServerApplicationRegistration -AppId $application.AppId
}

$application = Get-AzureAdApplication -Filter "identifierUris/any(uri:uri eq '$fhirAADUrl')"
if ($application) {
    Remove-FhirServerApplicationRegistration -AppId $application.AppId
}


$UserNamePrefix = "${EnvironmentName}-"
$userId = "${UserNamePrefix}admin"
$domain = $tenantInfo.TenantDomain
$userUpn = "${userId}@${domain}"

$aadUser = Get-AzureADUser -Filter "userPrincipalName eq '$userUpn'"
if ($aadUser) {
    Remove-AzureADUser -ObjectId $aadUser.ObjectId
}

$confidentialClientAppName = "${EnvironmentName}-confidential-client"
$confidentialClient = Get-AzureAdApplication -Filter "DisplayName eq '$confidentialClientAppName'"
if ($confidentialClient) {
    Remove-FhirServerApplicationRegistration -AppId $confidentialClient.AppId
}

$serviceClientAppName = "${EnvironmentName}-service-client"
$serviceClient = Get-AzureAdApplication -Filter "DisplayName eq '$serviceClientAppName'"
if ($confidentialClient) {
    Remove-FhirServerApplicationRegistration -AppId $serviceClient.AppId
}

$publicClientAppName = "${EnvironmentName}-public-client"
$publicClient = Get-AzureAdApplication -Filter "DisplayName eq '$publicClientAppName'"
if ($publicClient) {
    Remove-FhirServerApplicationRegistration -AppId $publicClient.AppId
}
