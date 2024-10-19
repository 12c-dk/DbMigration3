using Adapter.Sql.Managers;
using Dapper;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapter.Sql;

public class SqlAdapter : BaseAdapter
{
    private SqlAdapterConfig _config;
    SqlConnectionWrapper _sqlConnection;
    private readonly ILogger<SqlAdapter> _logger;

    public SqlAdapter(ILogger<SqlAdapter> logger)
    {
        _logger = logger;
    }

    public override async Task<DbOperationResponse> SetConfiguration<T>(T configuration)
    {
        if (configuration is SqlAdapterConfig config)
        {
            _config = config;

            try
            {
                _sqlConnection = new SqlConnectionWrapper(_config.ConnectionString);
                var testResult = await TestConnection();

                if (testResult)
                {
                    return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Info, "SetConfiguration completed successfully."));
                }
                else
                {
                    return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Error, "SetConfiguration cannot connect to database."));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"SetConfiguration cannot connect to database. {ex.Message}");
                return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Error, $"SetConfiguration cannot connect to database. {ex.Message}"));

            }
        }
        else
        {
            return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Error, "Invalid configuration type for SqlAdapter"));

        }
    }

    //SQL adapter skal ikke koncentrere sig om DbTableSchema. Det må håndteres i et DAL / repository lag
    public override async Task<List<DbItem>> GetTableData(string tableName, int? top = null, List<string> selectFields = null, string queryString = null)
    {
        if (queryString != null && queryString.ToLower().StartsWith("where "))
        {
            queryString = queryString.Substring(6);
        }

        //Check if selectFields exist in schema? This would require the DbTableSchema
        string schemaQuery = @$"SELECT
  	            {(top != null ? "top " + top : "")}
                {(selectFields != null ? string.Join(", ", selectFields) : "*")}
                FROM {tableName} 
                {(queryString != null ? $"WHERE {queryString}" : "")} ";

        IEnumerable<dynamic> rows = await _sqlConnection.QueryAsync(schemaQuery);

        List<DbItem> outputData = rows.ToDbItemsWithIdentifiers();
        return outputData;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="data"></param>
    /// <returns>Tuple of successItems as dictionary of input items and output items and DbOperationResponse with failed items</returns>
    public override async Task<DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>> InsertRows(string tableName, List<DbItem> data)
    {
        DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>> response = new DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>();

        //todo: add default value to fields. use in insert when value is null or not provided.

        try
        {
            //For each item, check that fields exist. 
            foreach (DbItem item in data)
            {
                try
                {

                    //Setup Insert statements and objects
                    DynamicParameters dbArgs = new DynamicParameters();
                    List<string> fieldSqlColumnNames = new List<string>();
                    List<string> fieldSqlVariableNames = new List<string>();

                    foreach (KeyValuePair<string, object> insertField in item.DataAndIdentifiers())
                    {
                        fieldSqlColumnNames.Add(insertField.Key);
                        fieldSqlVariableNames.Add($"@{insertField.Key}");

                        dbArgs.Add(insertField.Key, insertField.Value);
                    }

                    //Build output fields:
                    //We get primary keys from item. InsertRows might be used on tables without primary keys.
                    //Output all fields and add as outputItem.
                    string outputStatement = "OUTPUT INSERTED.*";

                    //Expected output "INSERT INTO TestTable (Name, Age) OUTPUT INSERTED.Id1, INSERTED.Id2 VALUES (@Name, @Age)";
                    string insertString = $"INSERT INTO {tableName} ({string.Join(",", fieldSqlColumnNames)}) {outputStatement} VALUES ({string.Join(",", fieldSqlVariableNames)} )";


                    //Execute insert
                    _logger.LogInformation($"Executing insert query: {insertString}");
                    var insertResult = (await _sqlConnection.QueryAsync(insertString, dbArgs)).ToList();

                    if (!insertResult.Any())
                    {
                        response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Inserting item failed. No Output Keys received. ", item.DataAndIdentifiers()));
                    }
                    else if (insertResult.Count > 1)
                    {
                        response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Inserting item failed. Multiple Output Keys received. ", item.DataAndIdentifiers()));
                    }
                    else if (insertResult.Count == 1)
                    {

                        List<Dictionary<string, object>> insertItemOutput = insertResult.DynamicToDictionary();

                        //We expect only one response row, since we insert one row at a time.
                        var outputValues = insertItemOutput.Single();

                        DbItem outputItem2 = new DbItem(null, outputValues);

                        response.ResponseValue.Add(item, outputItem2);
                        
                    }
                }
                catch (Exception ex)
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, $"INSERT failed {ex.Message}",item.DataAndIdentifiers()));
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="items"></param>
    /// <returns>Return tuple with success items and failed items. Success items should contain all the input items of successful items, plus output identifier fields.</returns>
    public override async Task<DbValueCollectionOperationResponse<List<DbItem>>> UpdateRows(string tableName, List<DbItem> items)
    {
        DbValueCollectionOperationResponse<List<DbItem>> response = new DbValueCollectionOperationResponse<List<DbItem>>();

        try
        {
            foreach (DbItem item in items)
            {
                DynamicParameters dbArgs = new DynamicParameters();
                List<string> setStatements = new List<string>();
                foreach (KeyValuePair<string, object> insertField in item.Data)
                {
                    setStatements.Add($"{insertField.Key} = @{insertField.Key}");

                    dbArgs.Add(insertField.Key, insertField.Value);
                }

                //Build query output statement:
                List<string> whereStatements = new List<string>();
                List<string> outputFieldStatements = new List<string>();

                foreach (var primaryKeyField in item.Identifiers)
                {
                    whereStatements.Add($"{primaryKeyField.Key} = @{primaryKeyField.Key}");
                    dbArgs.Add(primaryKeyField.Key, primaryKeyField.Value);

                    outputFieldStatements.Add($" INSERTED.{primaryKeyField.Key} ");

                }
                string outputStatement = $"OUTPUT {string.Join(",", outputFieldStatements)}";

                string updateString = $"UPDATE {tableName} SET {string.Join(", ", setStatements)} {outputStatement} WHERE {string.Join(" AND ", whereStatements)}";

                _logger.LogInformation($"Executing insert query: {updateString}");
                List<dynamic> sqlOutput = (await _sqlConnection.QueryAsync(updateString, dbArgs)).ToList();

                if (!sqlOutput.Any())
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Updating item failed. No Output Keys received. ", item.DataAndIdentifiers()));
                }
                else if (sqlOutput.Count > 1)
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "MULTIPLE rows updated. Identity field configuration has been invalid.", item.DataAndIdentifiers()));
                }
                else if (sqlOutput.Count == 1)
                {
                    List<Dictionary<string, object>> sqlItemOutput = sqlOutput.DynamicToDictionary();

                    //We expect only one response row, since we insert one row at a time. This holds the primary key fields
                    Dictionary<string, object> outputKeys = sqlItemOutput.First();
                    //This sets the DbItem Identifiers based on the sql output keys
                    //It really updates the values of the Identifiers. E.g. if the Identifier were changed by sql server on update
                    foreach (var key in outputKeys.Keys)
                    {
                        item.Identifiers[key] = outputKeys[key];
                    }
                    response.ResponseValue.Add(item);

                    //List<Dictionary<string, object>> insertItemOutput = sqlOutput.DynamicToDictionary();
                    //var outputValues = insertItemOutput.Single();

                    //DbItem outputItem2 = new DbItem(null, outputValues);

                    //response.ResponseValue.Add(outputItem2);
                }
            }

            return response;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    public async Task<List<string>> GetTables()
    {
        string sqlQuery = "SELECT CONCAT(s.name, '.', t.name) as TableName FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id order by s.name, t.name";

        IEnumerable<string> tables = await _sqlConnection.QueryAsync<string>(sqlQuery);
        return tables.ToList();
    }

    public Dictionary<string, object> GetItemPrimaryKeys(List<string> primaryKeys, DbItem item)
    {
        IEnumerable<KeyValuePair<string, object>> itemKeyFields = item.DataAndIdentifiers().Where(ipk =>
            primaryKeys.Any(pk => pk.ToLower() == ipk.Key.ToLower()));
        Dictionary<string, object> keyFieldsDict = itemKeyFields.ToDictionary(k => k.Key, v => v.Value);
        return keyFieldsDict;
    }

    public override async Task<DbValueCollectionOperationResponse<List<DbItem>>> DeleteItems(string tableName, List<DbItem> items)
    {
        DbValueCollectionOperationResponse<List<DbItem>> response = new DbValueCollectionOperationResponse<List<DbItem>>();

        try
        {
            foreach (DbItem item in items)
            {
                var primaryKeys = item.Identifiers.Keys.ToList();
                var keyFields = GetItemPrimaryKeys(primaryKeys, item);

                if (keyFields.Count == 0)
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Deleting item failed. No primary keys found.", item.DataAndIdentifiers()));
                    continue;
                }

                var whereClause = string.Join(" AND ", keyFields.Select(k => $"{k.Key} = @{k.Key}"));
                var deleteQuery = $"DELETE FROM {tableName} WHERE {whereClause}";

                _logger.LogInformation($"Executing delete query: {deleteQuery}");
                var result = await _sqlConnection.ExecuteAsync(deleteQuery, keyFields);

                if (result == 0)
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Deleting item failed. No rows affected.", item.DataAndIdentifiers()));
                }
                else
                {
                    response.ResponseValue.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            response.OperationResponse.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error,
                $"Unexpected error deleting data for table {tableName}. Exception message: {ex.Message}"));
        }

        return response;
    }

    public override async Task<bool> TestConnection()
    {
        try
        {
            string sqlQuery = "SELECT 1";
            IEnumerable<string> response = await _sqlConnection.QueryAsync<string>(sqlQuery);
            return response.Any();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"TestConnection could not connect to database. {ex.Message}");
            return false;
        }

    }
}