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

$extentions
Write-Output "Install.ps1 called. Initializing configuration"

"Changing directory to script path: $PSScriptRoot"
cd $PSScriptRoot

.\Initialize.ps1 -TargetJson $TargetJson -VariableGroupParameters $VariableGroupParameters

Write-Host -ForegroundColor Magenta "Install.ps1 VariableGroupParameters received"
# $VariableGroupParameters
# $global:VariableGroupParameters = $VariableGroupParameters

$DeployRoot = pwd
Write-Output "Deploying components"


cd 100-Infrastructure
Get-ChildItem "." -Recurse -Include "Deploy.ps1" | % { 
	Push-Location $_.DirectoryName
		Write-Output (pwd).Path
		.\Deploy.ps1  
	Pop-Location 
}
cd $DeployRoot

Write-Output "Deploying functionality"
cd 200-Functionality
Get-ChildItem "." -Recurse -Include "Deploy.ps1" | % { 
	Push-Location $_.DirectoryName
		Write-Output (pwd).Path
		.\Deploy.ps1  
	Pop-Location 
}
cd $DeployRoot
