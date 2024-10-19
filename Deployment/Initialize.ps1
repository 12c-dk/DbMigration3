param(
	[Parameter(Mandatory=$true, ValueFromPipeline = $true)]
	[String]$TargetJson,
	[switch]$AzureDeployment,
	[Hashtable] $VariableGroupParameters
)

if ($AzureDeployment)
{
	"This deployment is running from Azure. Setting AzureCreds."
	$global:AzureCreds = "is set"
}

Set-Location $PSScriptRoot
Import-Module ".\Modules\Azure.Deployment.psd1"

# VariableGroup parameters can be set in the deploy-azure.yml in the scriptArguments of the Deploy task. When passed on to Install.ps1 (which calls Initialize.ps1) they can be referenced from any deploy.ps1 script file using a reference like this: $VariableGroupParameters["UserName"]
"Got the following variable group parameters"
$VariableGroupParameters
$global:VariableGroupParameters = $VariableGroupParameters


write-host -foregroundcolor cyan "Using envronment config $TargetJson"
$global:Config = gc $TargetJson |Out-String | ConvertFrom-Json


Write-Output "import Azure PowerShell module"

Write-Output "Requesting Azure credentials."
if ($AzureCreds -eq $null)
{
	$global:AzureCreds = "is set"
	Connect-AzAccount -TenantId $config.TenantId
	Set-AzContext -SubscriptionId $config.SubscriptionId
}

#Select-AzSubscription  -SubscriptionId $Config.SubscriptionId
