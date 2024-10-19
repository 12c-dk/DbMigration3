Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host ("Creating Key Vault")

try {

    "The Azure context being used is:"
    $ctx =Get-AzContext
    $ctx

    $rg = $Config.ResourceGroupName

    $KeyVaultName = $config.Resource.KeyVaultName

    $servicePrincipalObjectId = $Config.ServiceConnectionObjectId

    $existingKeyVault = Get-AzResource -ResourceGroupName $rg -ResourceName $KeyVaultName

    if ($null -eq $existingKeyVault){
        $ParametersObj = @{
        "keyVaultName" = $KeyVaultName
        "objectId" = $servicePrincipalObjectId
        }
        "Executing keyvault arm deployment"
        New-AzResourceGroupDeployment -ResourceGroupName $rg -TemplateFile 'keyvaultcreate.json' -TemplateParameterObject $ParametersObj 
    }
    else {
        "KeyVault exists. Skipping execution of ARM template. "
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