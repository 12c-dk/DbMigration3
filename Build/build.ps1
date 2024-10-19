Write-Host "Start path: " + (pwd)

$buildPath = pwd
$target = "..\..\Deployment\200-Functionality\100-FunctionApp\functionapp"
$ProjectPath = (Resolve-Path ..\src\AzureFunctions.Api).path

cd $ProjectPath

Write-Host "Publishing website to folder"
dotnet publish -c Release -o $target

cd $buildPath