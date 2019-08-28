<#
.SYNOPSIS
Creates a new FHIR Server Samples environment.
.DESCRIPTION
#>
param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidateLength(5,12)]
    [ValidateScript({
        if ("$_" -Like "* *") {
            throw "Environment name cannot contain whitespace"
            return $false
        }
        else {
            return $true
        }
    })]
    [string]$EnvironmentName,

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentLocation = "westus",

    [Parameter(Mandatory = $false)]
    [string]$SourceRepository = "https://github.com/Microsoft/fhir-server-samples",

    [Parameter(Mandatory = $false)]
    [string]$SourceRevision = "master",

    [Parameter(Mandatory = $false)]
    [bool]$DeploySource = $true,

    [Parameter(Mandatory = $false)]
    [bool]$UsePaaS = $true,

    [Parameter(Mandatory = $false)]
    [ValidateSet('cosmos','sql')]
    [string]$PersistenceProvider = "cosmos",

    [Parameter(Mandatory = $false)]
    [ValidateSet('Stu3','R4')]
    [string]$FhirVersion = "R4",

    [Parameter(Mandatory = $false)]
    [SecureString]$SqlAdminPassword,

    [Parameter(Mandatory = $false)]
    [bool]$DeployAdf = $false,

    [parameter(Mandatory = $false)]
    [SecureString]$AdminPassword

)

Set-StrictMode -Version Latest

# Some additional parameter validation
if (($PersistenceProvider -eq "sql") -and ([string]::IsNullOrEmpty($SqlAdminPassword)))
{
    throw 'For SQL persistence provider you must provide -SqlAdminPassword parameter'
}

if ($UsePaaS -and ($PersistenceProvider -ne "cosmos"))
{
    throw 'SQL Server is only supported in OSS. Set -UsePaaS $false when using SQL'
}


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

if ($azureRmContext.Account.Type -eq "User") {
    Write-Host "Current context is user: $($azureRmContext.Account.Id)"
    
    $currentUser = Get-AzureRmADUser -UserPrincipalName $azureRmContext.Account.Id

    #If this is guest account, we will try a search instead
    if (!$currentUser) {
        $currentUser = Get-AzureRmADUser -SearchString $azureRmContext.Account.Id
    }

    $currentObjectId = $currentUser.Id

    if (!$currentObjectId) {
        throw "Failed to find objectId for signed in user"
    }
}
elseif ($azureRmContext.Account.Type -eq "ServicePrincipal") {
    Write-Host "Current context is service principal: $($azureRmContext.Account.Id)"
    $currentObjectId = (Get-AzureRmADServicePrincipal -ServicePrincipalName $azureRmContext.Account.Id).Id
}
else {
    Write-Host "Current context is account of type '$($azureRmContext.Account.Type)' with id of '$($azureRmContext.Account.Id)"
    throw "Running as an unsupported account type. Please use either a 'User' or 'Service Principal' to run this command"
}


# Set up Auth Configuration and Resource Group
./Create-FhirServerSamplesAuthConfig.ps1 -EnvironmentName $EnvironmentName -EnvironmentLocation $EnvironmentLocation -AdminPassword $AdminPassword -UsePaaS $UsePaaS

#Template URLs
$fhirServerTemplateUrl = "https://raw.githubusercontent.com/microsoft/fhir-server/master/samples/templates/default-azuredeploy.json"
if ($PersistenceProvider -eq 'sql')
{
    $fhirServerTemplateUrl = "https://raw.githubusercontent.com/microsoft/fhir-server/master/samples/templates/default-azuredeploy-sql.json"
}

$githubRawBaseUrl = $SourceRepository.Replace("github.com","raw.githubusercontent.com").TrimEnd('/')
$sandboxTemplate = "${githubRawBaseUrl}/${SourceRevision}/deploy/templates/azuredeploy-sandbox.json"
$dashboardJSTemplate = "${githubRawBaseUrl}/${SourceRevision}/deploy/templates/azuredeploy-fhirdashboard-js.json"
$importerTemplate = "${githubRawBaseUrl}/${SourceRevision}/deploy/templates/azuredeploy-importer.json"

$tenantDomain = $tenantInfo.TenantDomain
$aadAuthority = "https://login.microsoftonline.com/${tenantDomain}"

$dashboardJSUrl = "https://${EnvironmentName}dash.azurewebsites.net"

if ($UsePaaS) {
    $fhirServerUrl = "https://${EnvironmentName}.azurehealthcareapis.com"
} else {
    $fhirServerUrl = "https://${EnvironmentName}srvr.azurewebsites.net"
}

$confidentialClientId = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-id").SecretValueText
$confidentialClientSecret = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-confidential-client-secret").SecretValueText
$serviceClientId = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-id").SecretValueText
$serviceClientSecret = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-service-client-secret").SecretValueText
$serviceClientObjectId = (Get-AzureADServicePrincipal -Filter "AppId eq '$serviceClientId'").ObjectId
$dashboardUserUpn  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-upn").SecretValueText
$dashboardUserOid = (Get-AzureADUser -Filter "UserPrincipalName eq '$dashboardUserUpn'").ObjectId
$dashboardUserPassword  = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-admin-password").SecretValueText
$publicClientId = (Get-AzureKeyVaultSecret -VaultName "${EnvironmentName}-ts" -Name "${EnvironmentName}-public-client-id").SecretValueText

$accessPolicies = @()
$accessPolicies += @{ "objectId" = $currentObjectId.ToString() }
$accessPolicies += @{ "objectId" = $serviceClientObjectId.ToString() }
$accessPolicies += @{ "objectId" = $dashboardUserOid.ToString() }

#We need to pass "something" as SQL server password even when it is not used:
if ([string]::IsNullOrEmpty($SqlAdminPassword))
{
    $SqlAdminPassword = ConvertTo-SecureString -AsPlainText -Force "DummySQLServerPasswordNotUsed"
}

# Deploy the template
New-AzureRmResourceGroupDeployment -TemplateUri $sandboxTemplate -environmentName $EnvironmentName -ResourceGroupName $EnvironmentName -fhirServerTemplateUrl $fhirServerTemplateUrl -fhirVersion $FhirVersion -sqlAdminPassword $SqlAdminPassword -aadAuthority $aadAuthority -aadDashboardClientId $confidentialClientId -aadDashboardClientSecret $confidentialClientSecret -aadServiceClientId $serviceClientId -aadServiceClientSecret $serviceClientSecret -smartAppClientId $publicClientId -fhirDashboardJSTemplateUrl $dashboardJSTemplate -fhirImporterTemplateUrl $importerTemplate -fhirDashboardRepositoryUrl $SourceRepository -fhirDashboardRepositoryBranch $SourceRevision -deployDashboardSourceCode $DeploySource -usePaaS $UsePaaS -accessPolicies $accessPolicies -deployAdf $DeployAdf

Write-Host "Warming up site..."
Invoke-WebRequest -Uri "${fhirServerUrl}/metadata" | Out-Null
$functionAppUrl = "https://${EnvironmentName}imp.azurewebsites.net"
Invoke-WebRequest -Uri $functionAppUrl | Out-Null 

@{
    dashboardUrl              = $dashboardJSUrl
    fhirServerUrl             = $fhirServerUrl
    dashboardUserUpn          = $dashboardUserUpn
    dashboardUserPassword     = $dashboardUserPassword
}
