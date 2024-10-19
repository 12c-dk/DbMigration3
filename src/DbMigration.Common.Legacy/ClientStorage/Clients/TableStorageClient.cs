using DbMigration.Common.Legacy.ClientStorage.Model;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Model.MappingModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.ClientStorage.Clients
{
    public class TableStorageClient
    {
        private readonly ConfigManager _configManager;

        public TableStorageClient(StorageClient storageClient, string tableName, ConfigManager configManager)
        {
            _configManager = configManager;

            var tableClient = storageClient.CloudStorageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);
            try
            {
                Table.CreateIfNotExistsAsync().Wait();

            }
            catch (AggregateException e)
            {
                var innerException = e.InnerException;

                if (innerException != null && innerException.GetType() == typeof(StorageException))
                {
                    var storageException = (StorageException)innerException;
                    throw new ApplicationException("Error during table creation. " + storageException.RequestInformation?.ExtendedErrorInformation?.ErrorMessage, e);
                }

                throw new ApplicationException("Error during table creation. Unknown exception type. " + e.Message, e);

            }
        }

        public CloudTable Table { get; }



        public async Task<TableResult> Insert(ITableEntity entity)
        {
            TableOperation operation = TableOperation.Insert(entity);
            TableResult result = await Table.ExecuteAsync(operation);
            return result;
        }

        public async Task<TableResult> InsertOrMerge(ITableEntity entity)
        {
            TableOperation operation = TableOperation.InsertOrMerge(entity);
            TableResult result = await Table.ExecuteAsync(operation);
            return result;
        }

        public async Task<IList<TableResult>> InsertOrMergeBatch<T>(List<T> entities) where T : TableEntity
        {
            if (entities == null || entities.Count == 0)
            {
                return null;
            }

            List<TableResult> result = new List<TableResult>();

            //If using local azurite
            var usingAzurite = _configManager.GetConfigValue("UsingAzurite");
            if (!string.IsNullOrEmpty(usingAzurite) && usingAzurite.ToLower() == "true")
            {
                List<Task<TableResult>> insertTasks = new List<Task<TableResult>>();

                foreach (var entity in entities)
                {
                    TableOperation operation = TableOperation.InsertOrMerge(entity);
                    insertTasks.Add(Table.ExecuteAsync(operation));
                }
                await Task.WhenAll(insertTasks.ToArray());

                foreach (var insertTask in insertTasks)
                {
                    result.Add(insertTask.Result);
                }


                return result;
            }

            //If using Azure storage account
            TableBatchOperation batchOperation = new TableBatchOperation();

            var distinctPartitionKeys = entities.Select(e => e.PartitionKey).Distinct();

            foreach (string partitionKey in distinctPartitionKeys)
            {

                foreach (var entity in entities.Where(e => e.PartitionKey == partitionKey))
                {
                    batchOperation.Add(TableOperation.InsertOrMerge(entity));

                }
                IList<TableResult> batchResult = await Table.ExecuteBatchAsync(batchOperation);
                result.AddRange(batchResult);
            }

            return result;
        }

        public async Task<TableResult> MergeChange(ITableEntity entity)
        {
            TableOperation operation = TableOperation.Merge(entity);
            TableResult result = await Table.ExecuteAsync(operation);
            return result;
        }

        public async Task<TableResult> Delete(ITableEntity entity)
        {
            TableOperation operation = TableOperation.Delete(entity);

            TableResult result = await Table.ExecuteAsync(operation);

            return result;
        }

        public async Task<DynamicTableEntity> Retrieve(string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve(partitionKey, rowKey);
            TableResult result = await Table.ExecuteAsync(retrieveOperation);
            if (result == null) return null;
            return result.Result as DynamicTableEntity;
        }

        public async Task<T> Retrieve<T>(string partitionKey, string rowKey) where T : TableEntity
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult retrievedResult = await Table.ExecuteAsync(retrieveOperation);

            return retrievedResult.Result as T;
        }

        public async Task<List<T>> RetrieveBatch<T>(List<ITableEntity> entities) where T : TableEntity
        {
            List<T> result = new List<T>();

            var usingAzurite = _configManager.GetConfigValue("UsingAzurite");
            if (!string.IsNullOrEmpty(usingAzurite) && usingAzurite.ToLower() == "true")
            {
                foreach (var entity in entities)
                {
                    var fullTableEntity = await Retrieve<T>(entity.PartitionKey, entity.RowKey);
                    result.Add(fullTableEntity);
                }

                return result;
            }

            TableBatchOperation batchOperation = new TableBatchOperation();

            var distinctPartitionKeys = entities.Select(e => e.PartitionKey).Distinct();

            foreach (string partitionKey in distinctPartitionKeys)
            {
                foreach (var entity in entities.Where(e => e.PartitionKey == partitionKey))
                {
                    //Both of these approaches work with an Azure account. But not locally. 
                    batchOperation.Add(TableOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey));
                    //batchOperation.Retrieve<T>(entity.PartitionKey, entity.RowKey);
                }
                IList<TableResult> batchResult = await Table.ExecuteBatchAsync(batchOperation);

                var batchResultOfType = batchResult.Select(r => r.Result as T);
                result.AddRange(batchResultOfType);
                batchOperation = new TableBatchOperation();
            }

            return result;

        }

        /// <summary>
        /// This method queries items from table storage. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filterString">I.e. InternalTransactionId eq 'ef5140d7-cf6d-49db-9005-57bcaf6a4b99'</param>
        /// <param name="selectColumns">If set, only the columns selected will be returned</param>
        /// <param name="takeCount"></param>
        /// <returns>A List of items matching the query</returns>
        public List<T> Query<T>(string filterString = "", IList<string> selectColumns = null,
            int? takeCount = null) where T : ITableEntity, new()
        {
            TableQuery<T> query = new TableQuery<T>
            {
                FilterString = filterString
            };
            if (selectColumns != null)
                query.SelectColumns = selectColumns;

            if (takeCount != null)
                query.TakeCount = takeCount;

            TableContinuationToken continuationToken = null;
            List<T> queryOutput = new List<T>();

            do
            {
                var queryResult = Table.ExecuteQuerySegmentedAsync(query, continuationToken);
                var result = queryResult.Result;
                continuationToken = result.ContinuationToken;
                queryOutput.AddRange(queryResult.Result.Results.ToList());

            } while (continuationToken != null);

            return queryOutput;
        }

        /// <summary>
        /// This method queries items from table storage. 
        /// </summary>
        /// <param name="filterString">I.e. InternalTransactionId eq 'ef5140d7-cf6d-49db-9005-57bcaf6a4b99'</param>
        /// <param name="selectColumns">If set, only the columns selected will be returned</param>
        /// <param name="takeCount"></param>
        /// <returns>A List of items matching the query</returns>
        public List<DynamicTableEntity> Query(string filterString = "", IList<string> selectColumns = null,
            int? takeCount = null)
        {
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>
            {
                FilterString = filterString
            };
            if (selectColumns != null)
                query.SelectColumns = selectColumns;

            if (takeCount != null)
                query.TakeCount = takeCount;

            TableContinuationToken continuationToken = null;
            List<DynamicTableEntity> queryOutput = new List<DynamicTableEntity>();

            do
            {
                var queryResult = Table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.Result.ContinuationToken;
                queryOutput.AddRange(queryResult.Result.Results.ToList());

            } while (continuationToken != null);

            return queryOutput;
        }

        /// <summary>
        /// This method queries items from table storage and invokes callbackFunc with a batch of rows and a batchNumber that is increased with each batch. 
        /// </summary>
        /// <typeparam name="T">Can be DynamicTableEntity, TableEntity or a model that inherits from ITableEntity</typeparam>
        /// <param name="callbackFunc">Expects a method reference like 'ProcessQueryResults&lt;DynamicTableEntity&gt;' for a method with a signature like: void ProcessQueryResults&lt;T&gt;(List&lt;T&gt; queryResults, int batchNumber) where T : ITableEntity</param>
        /// <param name="filterString">E.g. InternalTransactionId eq 'ef5140d7-cf6d-49db-9005-57bcaf6a4b99'</param>
        /// <param name="selectColumns">If set, only the columns selected will be returned</param>
        /// <param name="batchSize">The batch size when querying and the amount of items returned in batch returned to callbackFunc</param>
        /// <param name="maxRows"></param>
        /// <returns>A List of items matching the query</returns>
        public async Task QueryWithCallbackAsync<T>(Action<List<T>, int> callbackFunc, string filterString = "", IList<string> selectColumns = null,
            int? batchSize = null, int? maxRows = null) where T : ITableEntity, new()
        {
            TableQuery<T> query = new TableQuery<T>
            {
                FilterString = filterString
            };
            if (selectColumns != null)
                query.SelectColumns = selectColumns;

            if (batchSize != null)
                query.TakeCount = batchSize;

            TableContinuationToken continuationToken = null;

            List<T> queryOutput = new List<T>();

            int batchNumber = 1;
            do
            {
                //Limit number of rows to maxRows
                if (maxRows != null && queryOutput.Count >= maxRows)
                {
                    break;
                }

                var queryResult = await Table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;

                queryOutput.AddRange(queryResult.Results.ToList());
                if (batchSize == null)
                {
                    callbackFunc(queryResult.Results, batchNumber);
                    batchNumber++;

                }
                else if (queryOutput.Count >= batchSize)
                {
                    callbackFunc(queryOutput, batchNumber);
                    queryOutput = new List<T>();
                    batchNumber++;
                }

            } while (continuationToken != null);


            if (queryOutput.Count > 0)
            {
                callbackFunc(queryOutput, batchNumber);
            }


        }

        /// <summary>
        /// This method queries items from table storage. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filterString">I.e. InternalTransactionId eq 'ef5140d7-cf6d-49db-9005-57bcaf6a4b99'</param>
        /// <param name="selectColumns">If set, only the columns selected will be returned</param>
        /// <param name="takeCount"></param>
        /// <returns>A List of items matching the query</returns>
        public async Task<List<T>> QueryAsync<T>(string filterString = "", IList<string> selectColumns = null,
            int? takeCount = null) where T : ITableEntity, new()
        {
            TableQuery<T> query = new TableQuery<T>
            {
                FilterString = filterString,
                SelectColumns = selectColumns, //May be null
                TakeCount = takeCount // May be null
            };

            TableContinuationToken continuationToken = null;
            List<T> transactionStatusList = new List<T>();

            do
            {
                var queryResult = await Table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                transactionStatusList.AddRange(queryResult.Results.ToList());

            } while (continuationToken != null);

            return transactionStatusList;
        }

        /// <summary>
        /// This method queries items from table storage. 
        /// </summary>
        /// <param name="filterString">I.e. InternalTransactionId eq 'ef5140d7-cf6d-49db-9005-57bcaf6a4b99'</param>
        /// <param name="selectColumns">If set, only the columns selected will be returned</param>
        /// <param name="takeCount"></param>
        /// <returns>A List of items matching the query</returns>
        public async Task<List<DynamicTableEntity>> QueryAsync(string filterString = "", IList<string> selectColumns = null,
            int? takeCount = null)
        {
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>
            {
                FilterString = filterString
            };
            if (selectColumns != null)
                query.SelectColumns = selectColumns;

            if (takeCount != null)
                query.TakeCount = takeCount;


            TableContinuationToken continuationToken = null;
            List<DynamicTableEntity> transactionStatusList = new List<DynamicTableEntity>();

            do
            {
                var queryResult = await Table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                transactionStatusList.AddRange(queryResult.Results.ToList());

            } while (continuationToken != null);

            return transactionStatusList;
        }

        public async Task<TableStorageSchema> ScanForFields(int rowCount = 100)
        {
            TableStorageSchema schema = new TableStorageSchema();

            var dynamicRowOutput = await QueryAsync(takeCount: rowCount);

            schema.Fields.Add(new TableStorageField()
            {
                FieldName = "PartitionKey",
                FieldType = EdmType.String,
                IsIdentifier = true
            });
            schema.Fields.Add(new TableStorageField()
            {
                FieldName = "RowKey",
                FieldType = EdmType.String,
                IsIdentifier = true
            });
            schema.Fields.Add(new TableStorageField()
            {
                FieldName = "Timestamp",
                FieldType = EdmType.DateTime,
                IsIdentifier = false
            });

            foreach (var dynamicRow in dynamicRowOutput)
            {
                foreach (var rowProperty in dynamicRow.Properties)
                {
                    EdmType propertyType = dynamicRow.Properties[rowProperty.Key].PropertyType;
                    string fieldName = rowProperty.Key;

                    if (!schema.Fields.Any(f => f.FieldName == fieldName && f.FieldType == propertyType))
                    {
                        schema.Fields.Add(new TableStorageField() { FieldName = fieldName, FieldType = propertyType });

                    }

                }
            }

            return schema;
        }

        public async Task<DbTableSchema> ScanForFields2(int rowCount = 100)
        {
            DbTableSchema schema = new DbTableSchema();

            var dynamicRowOutput = await QueryAsync(takeCount: rowCount);

            schema.EnsureTableField(new DbField()
            {
                Name = "PartitionKey",
                FieldType = EdmType.String,
                IsPrimaryKey = true
            });
            schema.EnsureTableField(new DbField()
            {
                Name = "RowKey",
                FieldType = EdmType.String,
                IsPrimaryKey = true
            });
            schema.EnsureTableField(new DbField()
            {
                Name = "Timestamp",
                FieldType = EdmType.DateTime,
                IsPrimaryKey = false
            });

            foreach (var dynamicRow in dynamicRowOutput)
            {
                foreach (var rowProperty in dynamicRow.Properties)
                {
                    EdmType propertyType = dynamicRow.Properties[rowProperty.Key].PropertyType;
                    string fieldName = rowProperty.Key;

                    if (!schema.Fields.Any(f => f.Name == fieldName && (EdmType)f.FieldType == propertyType))
                    {
                        schema.EnsureTableField(new DbField() { Name = fieldName, FieldType = propertyType });

                    }

                }
            }

            return schema;
        }
    }
}
