Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host ("Adding KeyVault secrets")

"KeyVault deployment got VariableGroupParameter"
$VariableGroupParameters

function initialize-KeyVaultSecret
{
	param(
        [Parameter(Mandatory = $true)] [string] $KeyVaultName,
        [Parameter(Mandatory = $true)] [string] $SecretName,
        [string] $Value = ""
    )
    "Checking if $SecretName secret exists"
    $KeyVaultSecret = Get-AzKeyVaultSecret -VaultName $KeyVaultName -Name $SecretName
    if ($null -eq $KeyVaultSecret)
    {
        if ([string]::IsNullOrEmpty($value))
        {
             $value = "Unset"
        }

        "Creating secret using Set-AzuKeyVaultSecret on $KeyVaultName"
        $Secret = ConvertTo-SecureString -String $value -AsPlainText -Force
        Set-AzKeyVaultSecret -VaultName $KeyVaultName -Name $SecretName -SecretValue $Secret 
    }
    else {
        if ([string]::IsNullOrEmpty($value)) {
            "Not setting KeyVault value because it already has a value set."
        }
        else {
            "Updating KeyVault value"
            $Secret = ConvertTo-SecureString -String $value -AsPlainText -Force
            Set-AzKeyVaultSecret -VaultName $KeyVaultName -Name $SecretName -SecretValue $Secret

        }
    }
}

try {

    $rg = $Config.ResourceGroupName

    $KeyVaultName = $config.Resource.KeyVaultName

    #Using VariableGroupParameter for setting KeyVault secret value
    # $ServiceUserName = ConvertTo-SecureString -String $VariableGroupParameters["ServiceUserName"] -AsPlainText -Force
    # Set-AzKeyVaultSecret -VaultName $KeyVaultName -Name "ServiceUserName" -SecretValue $ServiceUserName

    #Creating placeholders for KeyVault settings. These can be manually modified in the Azure Portal by the Environment owner. 
    foreach ($secretName in $config.Resource.KeyVaultSecretNames)
    {
        "Creating KeyVault secret: $secretName"
        initialize-KeyVaultSecret -KeyVaultName $KeyVaultName -SecretName $secretName
    }

}
catch {
    "Exception message: "
    $_.Exception.message
    Resolve-AzError
    $_ | fl
    ($error[0].Exception.Stacktrace | Out-String)
    $_.ScriptStackTrace | Out-String
    
    throw $_.Exception.message
}