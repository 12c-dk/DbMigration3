Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host ("Creating storage account")

$containerName = "containername"
$storageAccountName = $config.Resource.StorageAccountName

$ParametersObj = @{
    storageName   = $storageAccountName
    containerName = $containerName
}

New-AzResourceGroupDeployment -ResourceGroupName $config.ResourceGroupName  -TemplateFile 'storageaccount.json' -TemplateParameterObject $ParametersObj 

