<#
.SYNOPSIS
Adds the required application registrations and user profiles to an AAD tenant
.DESCRIPTION
#>
param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$EnvironmentName,

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentLocation = "westus2",

    [Parameter(Mandatory = $false)]
    [string]$SourceRepository = "https://github.com/Microsoft/fhir-server-samples",

    [Parameter(Mandatory = $false)]
    [string]$SourceRevision = "master",

    [Parameter(Mandatory = $false)]
    [bool]$DeploySource = $true,
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

# Set up Auth Configuration and Resource Group
./Create-FhirServerSamplesAuthConfig.ps1 -EnvironmentName $EnvironmentName -EnvironmentLocation $EnvironmentLocation

#Template URLs
$githubRawBaseUrl = $SourceRepository.Replace("github.com","raw.githubusercontent.com").TrimEnd('/')
$sandboxTemplate = "${githubRawBaseUrl}/${SourceRevision}/deploy/templates/azuredeploy-sandbox.json"
$dashboardTemplate = "${githubRawBaseUrl}/${SourceRevision}/deploy/templates/azuredeploy-fhirdashboard.json"

$tenantDomain = $tenantInfo.TenantDomain
$aadAuthority = "https://login.microsoftonline.com/${tenantDomain}"

$dashboardUrl = "https://${EnvironmentName}dash.azurewebsites.net"
$fhirServerUrl = "https://${EnvironmentName}srvr.azurewebsites.net"

$confidentialClientId = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-id").SecretValueText
$confidentialClientSecret = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-secret").SecretValueText
$dashboardUserUpn  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-upn").SecretValueText
$dashboardUserPassword  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-password").SecretValueText

# Deploy the template
New-AzureRmResourceGroupDeployment -TemplateUri $sandboxTemplate -environmentName $environmentName -ResourceGroupName $environmentName -aadAuthority $aadAuthority -aadDashboardClientId $confidentialClientId -aadDashboardClientSecret $confidentialClientSecret -fhirDashboardTemplateUrl $dashboardTemplate -fhirDashboardRepositoryUrl $SourceRepository -fhirDashboardRepositoryBranch $SourceRevision -deployDashboardSourceCode $DeploySource

@{
    dashboardUrl              = $dashboardUrl
    fhirServerUrl             = $fhirServerUrl
    dashboardUserUpn          = $dashboardUserUpn
    dashboardUserPassword     = $dashboardUserPassword
}
