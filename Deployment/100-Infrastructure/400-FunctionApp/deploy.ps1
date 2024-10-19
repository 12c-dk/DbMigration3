Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host ("Creating Function App")

$ServiceUsername = get-ParameterReferenceLatestFromKeyVault `
    -KeyVaultName $Config.Resource.KeyVaultName `
    -ParameterName "ServiceUsername"

$ParametersObj = @{
"storageAccountName" = $config.Resource.StorageAccountName
"FunctionAppName" = $config.Resource.FunctionAppName
"HostingPlanName" = $config.Resource.AppServicePlanName
"ApplicationInsightsName" = $config.Resource.applicationInsightsName
"ServiceUsername" = $ServiceUsername
}

#Switch to functionappAppServicePlan.json to deploy Function app with App service plan. 
New-AzResourceGroupDeployment -ResourceGroupName $config.ResourceGroupName -TemplateFile 'functionappConsumptionPlan.json' -TemplateParameterObject $ParametersObj 
#New-AzResourceGroupDeployment -ResourceGroupName $config.ResourceGroupName -TemplateFile 'functionappAppServicePlan.json' -TemplateParameterObject $ParametersObj 
