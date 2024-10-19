# data mapping 2

## Generic models

```
GenericTable : List<GenericRow>
    GenericRow : DictionaryCaseInsensitive<object> : Dictionary<string, object>

DbData : List<DbItem>
    Guid SchemaId
    DbItem : DictionaryCaseInsensitive<object>

DbValueOperationResponse<T>
    T ResponseValue
    DbOperationResponse OperationResponse
        List<GeneralError> GeneralResponses
        List<DbItemResponse> ItemResponses
            Dictionary<string, object> PrimaryKeys
            DbOperationResponseSeverity Severity
        public List<DbItem> SuccessItems

DbValueListOperationResponse<T>
    List<T> ResponseValue
    List<T> NoMatchItems
    DbOperationResponse OperationResponse 

DbValueMapOperationResponse<T>
    Dictionary<T, T> ResponseValue
    List<T> NoMatchItems
    DbOperationResponse OperationResponse

```


## Diagram: 

This is for the mapping - in the MappingModel folder, containing classes like DbDatabase, DbField, DbItem, DbSchema. 

```mermaid
classDiagram

class DbDatabase  {
    Contains a list of Schemas [tables]
}
class DbTableSchema {
    Refers to a table
    List< DbTableSchema > SchemaVersions
}
class SchemaVersion {
    VersionNumber
    List < Field > Fields
}
class AuthenticationMethod {
    
    Type AuthenticationType
    Endpoint
    Username - KeyVault reference
    Password - KeyVault reference

}

class DbData  {
    Contains a batch of items. e.g. 500 Items
    Has a reference to DbTableSchema, but is loosely coupled. 
    Will be stored in separate files / messages.
    ---
    SchemaReference
    SchemaVersionReference
    List < DbItem > Items
}
class Mapping {
    SchemaVersion Source
    SchemaVersion Target
    Dictionary < SourceField, TargetField> Mappings
    BuildAutoMappings()

}

class DbDatabaseRepository {
    
    List<Schema>
}

class DbTableSchemaRepository {
    
}

class DbField {
    
}

DbDatabaseRepository --o DbDatabase

DbTableSchemaRepository --o DbDatabaseRepository 

DbTableSchemaRepository --o DbTableSchema

DbDatabase --o DbTableSchema

DbTableSchema --o DbData
DbTableSchema --o DbData
DbTableSchema --o SchemaVersion
DbTableSchema --o Mapping 
SchemaSvc --o DbDatabase
DbDatabase --o AuthenticationMethod

```



