# Project dependencies

```Mermaid
graph LR;
    Adapter.Sql --> DbMigration.Sync
    DbMigration.Sync --> Domain
    Adapter.Sql.Tests --> Adapter.Sql
    AzureFunctions.Api --> DbMigration.Common
    AzureFunctions.Api.IntegrationTest --> SqlDapperClient
    AzureFunctions.Api.Tests --> AzureFunctions.Api
    DbMigration.Sync.Tests --> Adapter.Sql
    DbMigration.Sync.Tests --> DbMigration.Sync
    Dotnet.Test --> DbMigration.Common
    SqlDapperClient --> DbMigration.Common
```