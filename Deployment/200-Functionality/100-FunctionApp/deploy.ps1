Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)
Write-Host "Publishing Function app content from Src\AzureFunctions.Api\ to Function app in Azure"

$AzureFunctionName = $config.Resource.FunctionAppName
$resourceGroup = $config.ResourceGroupName

$zipFolder = "..\..\FunctionPublish\"
$targetFile = "FunctionPublish.zip"

New-AzureZipPackage -SourceDirectory $zipFolder -TargetFile $targetFile

write-host -foregroundcolor cyan "Publishing function app content"
Publish-AzWebapp -ResourceGroupName $resourceGroup -Name $AzureFunctionName -ArchivePath (Resolve-Path $targetFile).Path -Confirm:$false -Force

write-host -foregroundcolor cyan "Restarting web app"
Restart-AzWebApp -ResourceGroupName $resourceGroup -Name $AzureFunctionName
