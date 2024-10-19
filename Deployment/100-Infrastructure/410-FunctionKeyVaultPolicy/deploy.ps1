Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host ("Setting KeyVault policy for Opportunity function app identity ")

try {
    $KeyVaultName = $Config.Resource.KeyVaultName
    $FunctionAppName = $Config.Resource.FunctionAppName

    "Getting web app"
    $wa = Get-AzWebApp -Name $FunctionAppName

    "setting Access policy on $KeyVaultName for function app identity:"
    $wa.Identity.PrincipalId

    [string]$appPrincipalId = $wa.Identity.PrincipalId
    
    "setting Access policy on $KeyVaultName for function app identity $appPrincipalId"

    Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ObjectId $appPrincipalId -PermissionsToSecrets get -BypassObjectIdValidation
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