# Introduction 

This is a GitHub repo. 

The purpose of this project is development of a migration tool for migrating databases with on-the-fly transformation of data. 

This is a new version of DbMigration, with the purpose of making a simple synchronization. 

Concept based on: [Microsoft Sync Framework](Documentation/MicrosoftSyncFramework.md)

Key features: 
- The migration engine can be interrupted and will pick up where it left off.
- Migration errors are recorded, but doesn't halt the migration process.

## References

- [blazor-samples](https://github.com/dotnet/blazor-samples)

## Datamodel

## High level flow

Discovery. Used for both source and target. 
- Get Schema
- Checking connection. Check if read and/or write permissions are available. 
- Get all source id's
    - Populate sync status with missing id's.
- Get all target id's
    - Determine items for deletion. Put where? In sync status "outbox" table?

Sync
- Go through all source id's
    - Get source item
    - Get target item
    - Compare items
    - If different, update target item
- Go through Outbox
    - Delete target item

Error handling
- Categorize errors in transient and data errors. 


Projects: 

Sync framework: [DbMigration.Sync](src/DbMigration.Sync/README.md)

Sync Adapter: 

### Message handlers

## Infrastructure

ResourceGroup: 
https://portal.azure.com/#@f12c.onmicrosoft.com/resource/subscriptions/d3e92861-7740-4f9f-8cd2-bdfe8dd4bde3/resourceGroups/DbMigration/overview

Repository:
https://12c.visualstudio.com/Testprojects/_git/DbMigration2


Primary resources:
- App service running Blazor Web App
- Table Storage (Used for storing migration state and logs)
- Azure Functions (Running the migration engine jobs)

Phase two:
- Azure Cosmos DB (For testing CosmosDB migrations)
- SQL server (For  testing SQL server migrations)


## Devcontainer

See: [DevContainer.md](.devcontainer/DevContainer.md)


## Testing

Need to populate table storage with sample data.

See testing.http file at: [src/AzureFunctions.Api.Tests/testing.http](src/AzureFunctions.Api.Tests/testing.http)

Integration test project:

ConnectionString for Custom port storage connection
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10100/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10101/devstoreaccount1;TableEndpoint=http://127.0.0.1:10102/devstoreaccount1;
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10200/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10201/devstoreaccount1;TableEndpoint=http://127.0.0.1:10202/devstoreaccount1;
```

Connecting using Azure storage explorer: 

- Right click "Storage accounts" - "Connect to Azure storage"
- Select "Local storage emulator"
- Modify the Blob, queue and tables ports. 
- Click 'Next' and then 'Connect'


## Blazor server project

Setup tasks.json (/.vscode/tasks.json) watch task (Hot reload). 

### Commands run:

dotnet new sln --name BlazorSln
dotnet new gitignore
dotnet new blazorserver --name BlazorServer

dotnet sln .\BlazorSln.sln add .\BlazorServer\BlazorServer.csproj

VSCode F1 - .NET: Generate assets for build and debug

### Debugging

Requires: 
- Visual studio 17.5.1, 
- .net framework 7. 
- Enable 'Hot reload on save'

Open project in vscode and run devcontainer. 

Start debugging by hitting F5. 

To stop debugging kill the console by selecting VSCode Terminal and clicking the trash bin. 

Hot reload works better in vscode, than in Visual studio. 


## Todo

Setup simple synchronization of two tables or  table - SPList, using DbMigration model
    two way sync
    catching events
    Using delta endpoint(s)
    must handle folder structure - version 2



