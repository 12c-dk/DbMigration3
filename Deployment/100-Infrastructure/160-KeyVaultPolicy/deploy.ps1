Write-Host -ForegroundColor Magenta ("Executing: " + $PWD.Path)

"Get Azure context using Get-AzContext"
# $ctx =Get-AzContext
# $ctx

$KeyVaultName = $config.Resource.KeyVaultName



# "Getting AZ role assignment using Get-AzRoleAssignment"
# Get-AzRoleAssignment

# #Allow the current user access to KeyVault secrets. If deploying using a privileged user (i.e. from a local computer) the user needs permissions to create secrets (See 170-KeyVaultValues)
# $roleAssignment = Get-AzRoleAssignment |select -First 1
# "Selected RoleAssignment"
# $roleAssignment
# $currentUserObjectId = $roleAssignment.ObjectId
# "Adding KV policy for current user using objectId: $currentUserObjectId"
# Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ObjectId $currentUserObjectId -PermissionsToSecrets set,list,get -BypassObjectIdValidation -ErrorAction Continue


$servicePrincipalObjectId = $Config.ServiceConnectionObjectId
"Adding KV policy for servicePrincipal using objectId: $servicePrincipalObjectId"
Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ObjectId $servicePrincipalObjectId -PermissionsToSecrets set,list,get -BypassObjectIdValidation -ErrorAction Continue
