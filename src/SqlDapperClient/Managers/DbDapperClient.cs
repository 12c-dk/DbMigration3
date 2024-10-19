using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using NotImplementedException = System.NotImplementedException;
using Microsoft.IdentityModel.Tokens;
using DbMigration.Common.Legacy.Helpers.DictionaryHelpers;
using DbMigration.Common.Legacy.Model.MappingModel;
// ReSharper disable UnusedVariable

namespace SqlDapperClient.Managers
{
    /// <summary>
    /// This client handles SQL queries and provides standard DbData type operations. 
    /// </summary>
    public class DbDapperClient
    {
        readonly SqlConnectionWrapper _sqlConnection;
        private readonly DbDatabase _database;
        private readonly ILogger<DbDapperClient> _logger;

        public DbDapperClient(SqlConnectionWrapper sqlConnection, DbDatabase dbDatabase, ILogger<DbDapperClient> logger)
        {
            this._sqlConnection = sqlConnection;
            _database = dbDatabase;
            _logger = logger;
        }

        public async Task<DbTableSchema> GetTableSchema(string tableName)
        {
            string schemaName;
            string tableNameWithoutSchema;
            if (tableName.Contains("."))
            {
                schemaName = tableName.Split(".")[0];
                tableNameWithoutSchema = tableName.Substring(tableName.IndexOf('.') + 1);
            }
            else
            {
                schemaName = "dbo";
                tableNameWithoutSchema = tableName;
            }
            

            string schemaQuery = @$"SELECT
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.Column_default,
    c.character_maximum_length,
    c.numeric_precision,
    c.is_nullable,
    c.COLUMN_DEFAULT,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS PrimaryKey,
    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT
        ku.TABLE_CATALOG,
        ku.TABLE_SCHEMA,
        ku.TABLE_NAME,
        ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
) pk ON c.TABLE_CATALOG = pk.TABLE_CATALOG
    AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
    AND c.TABLE_NAME = pk.TABLE_NAME
    AND c.COLUMN_NAME = pk.COLUMN_NAME
WHERE 
	c.TABLE_SCHEMA = '{schemaName}' AND
	c.TABLE_NAME = '{tableNameWithoutSchema}'";

            

            var result = await _sqlConnection.QueryAsync<DbTableColumns>(schemaQuery);

            //_database.DatabaseRepository.Tables.GetDbTableSchemaFromId()

            DbTableSchema schema = new DbTableSchema()
            {
                //SchemaId = Guid.NewGuid(),
                TableName = tableName
            };
            //schema.ParentDbDatabase = _database;

            foreach (var column in result)
            {
                SqlDbType? sqlDbType = GetSqlDbType(column.DATA_TYPE);
                var field = new DbField(column.COLUMN_NAME, sqlDbType);

                if (column.CHARACTER_MAXIMUM_LENGTH != null)
                {
                    field.Length = column.CHARACTER_MAXIMUM_LENGTH;
                }

                if (column.PrimaryKey)
                {
                    field.IsPrimaryKey = true;
                }
                field.TargetDbColumnDefault = column.COLUMN_DEFAULT;
                if (column.IsIdentity)
                {
                    field.IsIdentity = true;
                }
                schema.EnsureTableField(field);
            }

            return schema;
        }

        public SqlDbType? GetSqlDbType(string dataType)
        {
            if (Enum.TryParse(typeof(SqlDbType), dataType, true, out object result))
            {
                if (result == null) return null;
                return (SqlDbType)result;
            }
            else
            {
                //This may be a user defined CLR type like 'geography'. Handling as null for now.
                return null;
            }
        }

        
        public async Task<DbData> GetTableData(DbTableSchema tableSchema, int? top = null, List<string> selectFields = null, string queryString = null)
        {
            tableSchema.Validate();

            if (queryString != null && queryString.ToLower().StartsWith("where "))
            {
                queryString = queryString.Substring(6);
            }
            
            //Check if selectFields exist in schema? This would require the DbTableSchema
            string schemaQuery = @$"SELECT
  	            {(top != null ? "top " + top  : "" )}
                {(selectFields != null ? string.Join(", ", selectFields) : "*")}
                FROM {tableSchema.TableName} 
                {(queryString != null ? $"WHERE {queryString}" : "")} ";

            var rows = await _sqlConnection.QueryAsync(schemaQuery);

            DbData outputDbData = new DbData(tableSchema.SchemaId, rows.ToDbItems());
            return outputDbData;
        }

        public async Task<List<string>> GetTables()
        {
            string sqlQuery = "SELECT CONCAT(s.name, '.', t.name) as TableName FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id order by s.name, t.name";

            IEnumerable<string> tables = await _sqlConnection.QueryAsync<string>(sqlQuery);
            return tables.ToList();

        }

        public async Task<DbOperationResponse> UpsertRows(string tableName, DbData data)
        {
            //Todo: Implement this function 

            DbOperationResponse response = new DbOperationResponse();
            //todo: add default value to fields. use in insert when value is null or not provided.

            try
            {
                //Get table schema
                DbTableSchema schema = _database.GetDbTableSchemaByTableName(tableName);
                if (schema == null)
                {
                    response.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error, $"Table schema {tableName} doesn't exist. Cannot validate inputs."));
                    return response;
                }

                schema.Validate();

                //todo: Check if there are any values. 


                //Get items that has PrimaryKeys
                //See readme for flow - Upsert section


                //For each item, check that fields exist. 
                //Get items that has PrimaryKeys and those that don't. 
                var (itemsWithKeys, itemsWithoutKeys) = schema.GetItemsWithPrimaryKeys(data);
                itemsWithoutKeys.ForEach(i => response.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error,"Items doesn't have the primary keys according to schema",i)));

                //Get primary keys for each item. itemsKeys contains only the primary keys of the items that has primary keys.
                //I think we need that values along with the primary keys, so this seems obsolete. 
                List<DbItem> itemsKeys = new List<DbItem>();
                itemsWithKeys.ForEach(item =>
                    itemsKeys.Add(
                        new DbItem(
                            schema.GetItemPrimaryKeys(item)) 
                ));
                
                //Get existing items from database
                var getItemsResponse = await GetItemsFromDbById(schema, itemsKeys);
                List<DbItem> existingItems = getItemsResponse.ResponseValue;
                List<DbItem> nonExistingItems = getItemsResponse.NoMatchItems;
                response.Append(getItemsResponse.OperationResponse);

                //existingItems needs to be updated
                //nonExistingItems needs to be inserted / created

                if (nonExistingItems != null && nonExistingItems.Count > 0)
                {
                    //This should only be used on items that doesn't exist and needs to be inserted
                    //Not implemented yet
                    //var (itemsWithRequiredUpdateFields, itemsWithoutRequiredUpdateFields) = schema.GetItemsWithRequiredInsertFields(itemsWithKeys);
                    //itemsWithoutRequiredUpdateFields.ForEach(I => response.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Item doesn't have the required insert fields", I.Values)));

                    DbData nonExistingData = new DbData(schema.SchemaId, nonExistingItems);

                    var insertResult = await InsertRows(tableName, nonExistingData);
                    response.Append(insertResult.OperationResponse);

                }

                if (existingItems != null && existingItems.Count > 0)
                {
                    //todo: Get the data items based on existingItems. Otherwise existing data will be updated with existing data. 

                    var (successUpdates, updateOperationResponse) = await UpdateRows(tableName, existingItems);

                }

            }
            catch (Exception ex)
            {
                response.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error,
                    $"Unexpected error inserting data for table {tableName}. Exception message: {ex.Message}"));
                return response;
            }

            return response;

        }

        
        /// <summary>
        /// Takes a list of DbItem. DbItem might have more fields, but input expects only Primary key fields are used for the query. Returns a tuple with success items (DbData) and failed items List<DbItem></DbItem>.
        /// </summary>
        /// <param name="schema">Schema is used for building select query. Not for validating if inputIdentifiers are only primary keys</param>
        /// <param name="inputIdentifiers"></param>
        /// <param name="selectFieldsList"></param>
        /// <returns>DbValueListOperationResponse with found items (ResponseValue), Items not found (NoMatchItems) and OperationResponse with errors</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<DbValueListOperationResponse<DbItem>> GetItemsFromDbById(DbTableSchema schema, List<DbItem> inputIdentifiers, List<string> selectFieldsList = null)
        {
            DbValueListOperationResponse<DbItem> response = new DbValueListOperationResponse<DbItem>();

            if (inputIdentifiers == null || inputIdentifiers.Count == 0)
            {
                response.OperationResponse.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error, "No input identifiers provided."));
                return response;
            }

            //Ensure that we have identifier fields for all items.
            var itemsWithNoIdentifiers = inputIdentifiers.Where(i => i.IsNullOrEmpty()).ToList();
            itemsWithNoIdentifiers.ForEach(i => response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "GetItemsFromDbById got item with no values",i)));
            itemsWithNoIdentifiers.ForEach(i => inputIdentifiers.Remove(i));


            // ### BUILD QUERY ###

            //Build Query with the following format
            //SELECT BusinessEntityId, RateChangeDate from HumanResources.EmployeePayHistory h
            //where
            //(BusinessEntityId = 1 and RateChangeDate = '2009-01-14 00:00:00.000') or
            //(BusinessEntityId = 2 and RateChangeDate = '2008-01-31 00:00:00.000')

            //Build parameters and variables collections for query
            var (sqlItemsVariables, dapperParameters, variablesPrint) = GetDynamicParametersForQuery(schema, inputIdentifiers);

            string selectFields;
            if (selectFieldsList == null || selectFieldsList.IsNullOrEmpty())
                selectFields = string.Join(", ", schema.FieldsReadOnly.Select(f => f.Name));
            else
                selectFields = string.Join(", ", selectFieldsList);

            //Build 'where in' sql statement based on sqlVariables
            List<string> itemsInStatements = new List<string>();
            foreach (var sqlItemVariables in sqlItemsVariables)
            {
                var listOfInStatementsPerItem = sqlItemVariables.Select(v => $"{v.Key} = {v.Value}");
                itemsInStatements.Add($"( {string.Join(" AND ", listOfInStatementsPerItem)} )");
                //Expected output: a List of string containing items like: (Id1 = @Id1_1 AND Id2 = @Id2_1)
            }
            string selectString = $"SELECT {selectFields} from {schema.TableName} " +
             $"WHERE ";
            selectString += string.Join(" OR ", itemsInStatements);

            _logger.LogInformation("Executing query: ");
            _logger.LogInformation(variablesPrint);
            _logger.LogInformation(selectString);

            // ### EXECUTE QUERY ###
            IEnumerable<dynamic> sqlResult = await _sqlConnection.QueryAsync(selectString, dapperParameters);
            DbData outputDbData = new DbData(schema.SchemaId, sqlResult.ToDbItems());

            // ### SETUP RESPONSE ###
            DbData inputDbData = new DbData(schema.SchemaId, inputIdentifiers);
            var primaryKeys = schema.FieldsReadOnly.Where(f => f.IsPrimaryKey).Select(k => k.Name).ToList();

            DbValueMapOperationResponse<DbItem> rowMapResponse = FindRowsMap.FindMatchingRowMap(inputDbData, outputDbData, primaryKeys);
            Dictionary<DbItem, DbItem> matchingRowsMap = rowMapResponse.ResponseValue;
            List<DbItem> noMatchItems = rowMapResponse.NoMatchItems;

            response.ResponseValue = matchingRowsMap.Values.ToList();
            response.NoMatchItems = noMatchItems;
            response.OperationResponse.Append(rowMapResponse.OperationResponse);

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Schema field types are used to build print output</param>
        /// <param name="items"></param>
        /// <returns>List of Variable sets, one Variable set per input item. Variable set contains Dictionary of Item.Key (e.g Id1) and Sql variablename (e.g. @Id1_1). Also returns DynamicParameters for Sql query and string of parameters for output printing. </returns>
        public (List<Dictionary<string, string>>, DynamicParameters, string) GetDynamicParametersForQuery(DbTableSchema schema, List<DbItem> items)
        {
            DynamicParameters dynamicParameters = new DynamicParameters();
            List<Dictionary<string, string>> sqlVariables = new List<Dictionary<string, string>>();

            var parameterPrintBuilder = new StringBuilder();
            int inCount = 0;
            foreach (DbItem item in items)
            {
                inCount++;
                Dictionary<string, string> itemSqlVariables = new Dictionary<string, string>();

                foreach (var itemValue in item)
                {
                    string parameterName = $"{itemValue.Key}_{inCount}";
                    string parameterVariableName = $"@{itemValue.Key}_{inCount}";

                    //Add parameter for building query
                    dynamicParameters.Add(parameterName, itemValue.Value);

                    //Build itemSqlVariables.Key (e.g Id1) and itemSqlVariables.Value (e.g. @Id1_1).
                    itemSqlVariables.Add(itemValue.Key, parameterVariableName);

                    //Build parameter print output
                    var field = schema.FieldsReadOnly.FirstOrDefault(f => string.Equals(f.Name, itemValue.Key, StringComparison.CurrentCultureIgnoreCase));
                    string fieldType = field?.FieldType.ToString() ?? "int";
                    var pValue = dynamicParameters.Get<dynamic>(parameterName);
                    parameterPrintBuilder.AppendFormat("declare {0} {1} = '{2}'\n", parameterVariableName, fieldType, pValue.ToString());
                }
                sqlVariables.Add(itemSqlVariables);
                
            }

            return (sqlVariables, dynamicParameters, parameterPrintBuilder.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <returns>Return tuple with success items and failed items. Success items should contain all the input data of successful items, plus output identifier fields.</returns>
        public async Task<(DbData, DbOperationResponse)> UpdateRows(string tableName, List<DbItem> data)
        {
            //Check
            //Get table schema

            //Per item
            //Check if item has primary keys
            //Remove identity fields from input (auto numbering fields that can't be updated)
            //Remove non-schema fields - based on configuration
            
            //Prepare query
            //Isolate primary keys - needed for where clause
            //isolate other fields - needed for set clause
            //Build update query
            //Execute update query


            DbOperationResponse response = new DbOperationResponse();
            try
            {
                DbTableSchema schema = _database.GetDbTableSchemaByTableName(tableName);
                if (schema == null)
                {
                    response.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error, $"Table schema {tableName} doesn't exist. Cannot validate inputs."));
                    return (new DbData(Guid.Empty), response);
                }

                foreach (DbItem dbItem in data)
                {
                    //Check if item has primary keys
                    //Identity (auto numbering) field check - already implemented
                    //Separate primary keys from other fields 

                    if (!schema.ItemHasPrimaryKeys(dbItem))
                    {
                        response.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Item doesn't have primary keys according to schema", dbItem));
                        continue;
                    }

                    //Get Insert fields from DbItem based on schema definition
                    //Todo: Identifier fields might also be required for insert. If field is not a required field?

                    
                    //Filter out identity fields and include only schema fields - based on configuration
                    var itemFieldsMatchingSchemaResponse = schema.GetItemValuesMatchingSchemaFields(dbItem);
                    if (itemFieldsMatchingSchemaResponse.OperationResponse != null &&
                        itemFieldsMatchingSchemaResponse.OperationResponse.ItemResponses.Any(
                            r => r.Severity == DbOperationResponseSeverity.Error)
                        )
                    {
                        response.Append(itemFieldsMatchingSchemaResponse.OperationResponse);
                        continue;
                    }
                    var itemFieldsMatchingSchema = itemFieldsMatchingSchemaResponse.ResponseValue;


                    //Get non-primary key valueFields
                    //Logic: If Schema has PrimaryKey that matches valueField, exclude field from valueFields
                    IEnumerable<KeyValuePair<string, object>> valueFields = itemFieldsMatchingSchema.Where(itemField =>
                        !schema.FieldsReadOnly.Any(schemaField =>
                            schemaField.IsPrimaryKey &&
                            string.Equals(
                                schemaField.Name, 
                                itemField.Key, 
                                StringComparison.CurrentCultureIgnoreCase)));
                    
                    //This doesn't return identity fields if they are not on DbItem
                    //var primaryKeys = schema.GetItemPrimaryKeys(new DbItem() { Values = schemaFieldValues.ResponseValue });
                    var primaryKeys = schema.GetItemPrimaryKeys(dbItem);


                    DynamicParameters dbArgs = new DynamicParameters();
                    List<string> setStatements = new List<string>();
                    foreach (KeyValuePair<string, object> insertField in valueFields)
                    {
                        setStatements.Add($"{insertField.Key} = @{insertField.Key}");

                        dbArgs.Add(insertField.Key, insertField.Value);
                    }

                    //Build query output statement:
                    List<string> whereStatements = new List<string>();
                    List<string> outputFieldStatements = new List<string>();

                    foreach (var primaryKeyField in primaryKeys)
                    {
                        outputFieldStatements.Add($" INSERTED.{primaryKeyField.Key} ");
                        
                        whereStatements.Add($"{primaryKeyField.Key} = @{primaryKeyField.Key}");
                        dbArgs.Add(primaryKeyField.Key, primaryKeyField.Value);

                    }
                    string outputStatement = $"OUTPUT {string.Join(",", outputFieldStatements)}";


                    string updateString = $"UPDATE {tableName} SET {string.Join(", ", setStatements)} {outputStatement} WHERE {string.Join(" AND ", whereStatements)}";


                    _logger.LogInformation($"Executing insert query: {updateString}");
                    var itemUpdateResult = (await _sqlConnection.QueryAsync(updateString, dbArgs)).ToList();

                    if (!itemUpdateResult.Any())
                    {
                        response.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Updating item failed. No Output Keys received. ", dbItem));
                    }
                    else if (itemUpdateResult.Count > 1)
                    {
                        response.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "MULTIPLE rows updated. Identity field configuration has been invalid.", dbItem));
                    }
                    else if (itemUpdateResult.Count == 1)
                    {
                        List<Dictionary<string, object>> updateItemOutput = itemUpdateResult.DynamicToDictionary();

                        //We expect only one response row, since we insert one row at a time.
                        var outputKeys = updateItemOutput.First();
                        foreach (var key in outputKeys.Keys)
                        {
                            dbItem[key] = outputKeys[key];
                        }

                        response.SuccessItems.Add(dbItem);
                    }
                }
                
                var result = (new DbData(schema.SchemaId, response.SuccessItems), response);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <returns>Tuple of successItems as dictionary of input items and output items and DbOperationResponse with failed items</returns>
        public async Task<DbValueOperationResponse<Dictionary<DbItem, DbItem>>> InsertRows(string tableName, DbData data)
        {
            DbValueOperationResponse< Dictionary < DbItem, DbItem >> response = new DbValueOperationResponse<Dictionary<DbItem, DbItem>>();

            //todo: add default value to fields. use in insert when value is null or not provided.

            try
            {
                DbTableSchema schema = _database.GetDbTableSchemaByTableName(tableName);
                if (schema == null)
                {
                    response.OperationResponse.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error, $"Table schema {tableName} doesn't exist. Cannot validate inputs."));
                    return response;
                }

                //todo: Check if there are any values. 


                //For each item, check that fields exist. 
                foreach (DbItem item in data)
                {
                    //Get Insert fields from DbItem based on schema definition
                    //Todo: Identifier fields might also be required for insert. If field is not a required field?


                    //Validate / handle Identity fields
                    //The next statement GetItemValuesMatchingSchemaFields gets fields without identity fields. Does this make the following check obsolete? Not according to copilot. 
                    if (schema.ItemHasIdentityFields(item))
                    {
                        var itemFields = schema.GetFieldsFromItem(item);
                        if (schema.Config.OnInsertFieldsNotInSchemaResponse == DbTableSchemaReaction.Error)
                        {
                            string identityFields = string.Join(", ", itemFields.Where(f => f.IsIdentity).Select(f => f.Name)); 
                            response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, $"Item contains Identity fields: {identityFields}. Identity fields cannot be inserted",item));
                            continue;
                        }
                        else if (schema.Config.OnInsertFieldsNotInSchemaResponse == DbTableSchemaReaction.Ignore)
                        {
                            foreach (DbField dbField in itemFields.Where(f => f.IsIdentity))
                            {
                                item.Remove(dbField.Name);
                            }
                        }
                        else if (schema.Config.OnInsertFieldsNotInSchemaResponse == DbTableSchemaReaction.Include)
                        {
                            //Do nothing, already included
                        }
                    }

                    var insertFieldsResponse = schema.GetItemValuesMatchingSchemaFields(item);
                    if (insertFieldsResponse.OperationResponse != null && 
                        insertFieldsResponse.OperationResponse.ItemResponses.Any(
                            r => r.Severity == DbOperationResponseSeverity.Error)
                        )
                    {
                        response.OperationResponse.Append(insertFieldsResponse.OperationResponse);
                        continue;
                    }


                    //Setup Insert statements and objects
                    DynamicParameters dbArgs = new DynamicParameters();
                    List<string> fieldSqlColumnNames = new List<string>();
                    List<string> fieldSqlVariableNames = new List<string>();

                    foreach (KeyValuePair<string, object> insertField in insertFieldsResponse.ResponseValue)
                    {
                        //Check if key exists in schema
                        //If not, add to schema
                        //If yes, check type
                        //If type is different, throw exception

                        fieldSqlColumnNames.Add(insertField.Key);
                        fieldSqlVariableNames.Add($"@{insertField.Key}");

                        dbArgs.Add(insertField.Key, insertField.Value);
                    }

                    //Build output fields:
                    //We get primary keys from item. We don't care if schema doesn't have PrimaryKeys. InsertRows might be used on tables without primary keys.
                    //Output all fields and add as outputItem.
                    string outputStatement = "OUTPUT INSERTED.*";

                    //Expected output "INSERT INTO TestTable (Name, Age) OUTPUT INSERTED.Id1, INSERTED.Id2 VALUES (@Name, @Age)";
                    string insertString = $"INSERT INTO {tableName} ({string.Join(",", fieldSqlColumnNames)}) {outputStatement} VALUES ({string.Join(",", fieldSqlVariableNames)} )";


                    //Execute insert
                    _logger.LogInformation($"Executing insert query: {insertString}" );
                    var insertResult = (await _sqlConnection.QueryAsync(insertString, dbArgs)).ToList();

                    if (!insertResult.Any())
                    {
                        response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Inserting item failed. No Output Keys received. ", item));
                    }
                    else if (insertResult.Count > 1)
                    {
                        response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Inserting item failed. Multiple Output Keys received. ", item));
                    }
                    else if (insertResult.Count == 1)
                    {
                        List<Dictionary<string, object>> insertItemOutput = insertResult.DynamicToDictionary();
                        
                        //We expect only one response row, since we insert one row at a time.
                        var outputValues = insertItemOutput.Single();
                        DbItem outputItem = new DbItem(outputValues);
                        
                        response.OperationResponse.SuccessItems.Add(item);
                        
                        response.ResponseValue.Add(item, outputItem);
                    }
                }
            }
            catch (Exception ex)
            {
                response.OperationResponse.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error,
                    $"Unexpected error inserting data for table {tableName}. Exception message: {ex.Message}"));
                return response;
            }

            return response;
        }

        // ReSharper disable once UnusedMember.Local
        private string GetFieldTypeDelimeter(SqlDbType fieldType)
        {
            

            string delimiter = "";
            switch (fieldType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Binary:
                case SqlDbType.Decimal:
                case SqlDbType.Float:
                case SqlDbType.Int:
                case SqlDbType.Money:
                case SqlDbType.SmallInt:

                case SqlDbType.Bit:


                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Char:
                    // Code for SqlDbType.Char
                    delimiter = "'";
                    break;


                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.SmallDateTime:

                case SqlDbType.Image:
                case SqlDbType.Real:
                case SqlDbType.SmallMoney:
                case SqlDbType.Structured:
                case SqlDbType.Text:
                case SqlDbType.Time:
                case SqlDbType.Timestamp:
                case SqlDbType.TinyInt:
                case SqlDbType.Udt:
                case SqlDbType.UniqueIdentifier:
                case SqlDbType.VarBinary:
                case SqlDbType.VarChar:
                case SqlDbType.Variant:
                case SqlDbType.Xml:
                default:
                    // Code for unknown SqlDbType
                    break;
            }

            return delimiter;

        }

        //Intended operations:
        //v - GetTables
        //v - GetTableSchema 
        //v - GetTableData - already have GetTableData (List<string> selectFields = null, string queryString = null, int top = 1000 ) # queryString is the db specific query language
        //GetTableDataBatched - takes delegate for returning data in batches
        //InsertRows(List<DbItem> dbItems)
        //DeleteRows(List<Dictionary<string, object>>) - takes list of key value pairs to identify the rows to delete. Needs schema to validate if all unique keys are provided.
        //CreateTable. Need to be able to set columns


    }
}
