function New-AzureZipPackage
{
	param(
		[string] $SourceDirectory,
		[string] $TargetFile
		)
	Write-Host -ForegroundColor Cyan "New-AzureZipPackage: Packaging $SourceDirectory for Azure deployment"

	if (Test-Path ($TargetFile))
	{
		rm ($TargetFile)
	}
	
	$excluded = @(".vscode", ".gitignore", "appsettings.json", "secrets","obj","*.csproj","*.user","*.sln","*.cs")
	$include = Get-ChildItem $SourceDirectory -Exclude $excluded
	Compress-Archive -Path $include -Update -DestinationPath $TargetFile

}

#######################################
### Kudu functions for getting keys ###
#######################################

function Get-PublishingProfileCredentials($resourceGroupName, $webAppName){
 
    $resourceType = "Microsoft.Web/sites/config"
    $resourceName = "$webAppName/publishingcredentials"
 
    $publishingCredentials = Invoke-AzResourceAction -ResourceGroupName $resourceGroupName -ResourceType $resourceType -ResourceName $resourceName -Action list -ApiVersion 2015-08-01 -Force
 
    return $publishingCredentials
}

function Get-KuduApiAuthorisationHeaderValue($resourceGroupName, $webAppName){

    $publishingCredentials = Get-PublishingProfileCredentials $resourceGroupName $webAppName
    return ("Basic {0}" -f [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $publishingCredentials.Properties.PublishingUserName, $publishingCredentials.Properties.PublishingPassword))))
}

function Get-MasterAPIKey($kuduApiAuthorisationToken, $webAppName ){
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $apiUrl = "https://$webAppName.scm.azurewebsites.net/api/functions/admin/masterkey"
    $result = Invoke-RestMethod -Uri $apiUrl -Headers @{"Authorization"=$kuduApiAuthorisationToken;"If-Match"="*"} 
    return $result`
}

function Get-HostAPIKeys($kuduApiAuthorisationToken, $webAppName, $masterKey ){
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $apiUrl = "https://$webAppName.azurewebsites.net/admin/host/keys?code=$masterKey"
    $result = Invoke-WebRequest $apiUrl
    return $result`
}


function Get-FunctionApiKeys($webAppName,$functionName,$masterKey) {
	#https://<FunctionAppname>.azurewebsites.net/admin/functions/<functionname>/KEYS?CODE=<MasterKeyCode>
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $apiUrl = "https://$webAppName.azurewebsites.net/admin/functions/$functionName/keys?code=$masterKey"
    $result = Invoke-WebRequest $apiUrl
    return $result`
}

function Get-FunctionAppUrl {
	param(
	[string]$resourceGroupName,
	[string]$webAppname,
	[string]$functionName
	)

	$accessToken = Get-KuduApiAuthorisationHeaderValue $resourceGroupName $webAppname
	$adminCode = Get-MasterAPIKey $accessToken $webAppname
	$result = Get-HostAPIKeys $accessToken $webAppname $adminCode.Masterkey
	$keysCode =  $result.Content | ConvertFrom-Json

	$functionKeyResult = Get-FunctionApiKeys $webAppname $functionName $adminCode.Masterkey
	$functionKeys = $functionKeyResult.Content | ConvertFrom-Json
	$httpTriggerKey = $functionKeys.keys[0].value

	$functionUrl = "https://$webAppname.azurewebsites.net/api/$functionName" + "?code=$httpTriggerKey"

	return $functionUrl
}

function get-ParameterReferenceLatestFromKeyVault
{
	param(
	[Parameter(Mandatory = $true)] 
	[String]$KeyVaultName,
	[Parameter(Mandatory = $true)] 
	[String]$ParameterName
	)
	$ErrorActionPreference = "Stop"

	$kvSecret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $ParameterName

	#Remove version number from path
	$noVersionSecret = $kvSecret.id.Substring(0,$kvSecret.id.LastIndexOf('/')+1)

	if ($null -eq $noVersionSecret)
	{ write-error "Could'nt get $ParameterName from keyvault"  }

	$kvReference = "@Microsoft.KeyVault(SecretUri=" + $noVersionSecret + ")"

	return $kvReference
}

function get-ParameterValueFromKeyVault
{
	param(
	[Parameter(Mandatory = $true)] 
	[String]$KeyVaultName,
	[Parameter(Mandatory = $true)] 
	[String]$ParameterName
	)
	$ErrorActionPreference = "Stop"

	$kvSecret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $ParameterName
	if ($kvSecret -eq $null)
	{write-error "Could'nt get $ParameterName from keyvault"  }
	$SecureString = $kvSecret.SecretValue

	$SecretText = [System.Net.NetworkCredential]::new("", $SecureString).Password
	if ([string]::IsNullOrEmpty($SecretText)) {
		write-error "SecretText is null or empty" 
	}

	return $SecretText
}



##usage:
#$resourceGroupName = $PA_EnvironmentVars.Configuration.Azure.ResourceGroupName
#$webAppname = $PA_EnvironmentVars.Configuration.Azure.ReceiveAzureFunction.name
#$functionName = "Httptrigger"

#$accessToken = Get-KuduApiAuthorisationHeaderValue $resourceGroupName $webAppname
#$adminCode = Get-MasterAPIKey $accessToken $webAppname
#Write-Host "masterKey = " $adminCode.Masterkey
#$result = Get-HostAPIKeys $accessToken $webAppname $adminCode.Masterkey
#$keysCode =  $result.Content | ConvertFrom-Json
#Write-Host "default Key = " $keysCode.Keys[0].Value

#$functionKeyResult = Get-FunctionApiKeys $webAppname $functionName $adminCode.Masterkey
#$functionKeys = $functionKeyResult.Content | ConvertFrom-Json
#$httpTriggerKey = $functionKeys.keys[0].value

# OR #

#Get-FunctionAppUrl $resourceGroupName $webAppname "HttpTrigger"


