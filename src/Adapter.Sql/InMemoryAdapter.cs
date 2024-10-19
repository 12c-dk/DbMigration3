using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Adapter.Sql.Managers;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;
using Microsoft.Extensions.Logging;

namespace Adapter.Sql
{
    public class InMemoryAdapter : BaseAdapter
    {
        private InMemoryAdapterConfig _config;
        private readonly List<DbItem> _inMemoryData = new List<DbItem>();

        public override async Task<DbOperationResponse> SetConfiguration<T>(T configuration)
        {
            // No configuration needed for in-memory adapter
            if (configuration is InMemoryAdapterConfig config)
            {
                _config = config;
                var testResult = await TestConnection();
                if (testResult)
                {
                    return new DbOperationResponse();
                }
                else
                {
                    return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Error, "SetConfiguration could not connect to database."));
                }
            }
            else
            {
                return new DbOperationResponse(new GeneralError(DbOperationResponseSeverity.Error, "SetConfiguration could not connect to database."));
            }
        }

        public override async Task<List<DbItem>> GetTableData(string tableName, int? top = null, List<string> selectFields = null, string queryString = null)
        {
            IEnumerable<DbItem> query = _inMemoryData;

            if (!string.IsNullOrEmpty(queryString))
            {
                // Parse the query string and apply conditions
                var conditions = queryString.Split(new[] { " AND ", " OR " }, StringSplitOptions.None);
                foreach (var condition in conditions)
                {
                    var parts = condition.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var field = parts[0].Trim();
                        var value = parts[1].Trim();

                        // Remove quotes around the value if present
                        if (value.StartsWith("'") && value.EndsWith("'"))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        // Apply the condition using LINQ
                        query = query.Where(item =>
                            (item.Data.ContainsKey(field) && item.Data[field].ToString() == value) ||
                            (item.Identifiers.ContainsKey(field) && item.Identifiers[field].ToString() == value));
                    }
                }
            }

            // Apply top if specified
            if (top.HasValue)
            {
                query = query.Take(top.Value);
            }

            return await Task.FromResult(query.ToList());
        }

        public override async Task<DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>> InsertRows(string tableName, List<DbItem> data)
        {
            var response = new DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>();

            foreach (var item in data)
            {
                if (_inMemoryData.Any(existingItem => existingItem.Identifiers.SequenceEqual(item.Identifiers)))
                {
                    throw new InvalidOperationException("An item with the same identifiers already exists.");
                }

                _inMemoryData.Add(item);
                response.ResponseValue.Add(item, item);
            }

            return await Task.FromResult(response);
        }

        public override async Task<DbValueCollectionOperationResponse<List<DbItem>>> UpdateRows(string tableName, List<DbItem> items)
        {
            var response = new DbValueCollectionOperationResponse<List<DbItem>>();

            foreach (var item in items)
            {
                var existingItem = _inMemoryData.FirstOrDefault(existing => existing.Identifiers.SequenceEqual(item.Identifiers));
                if (existingItem != null)
                {
                    existingItem.Data = item.Data;
                    response.ResponseValue.Add(existingItem);
                }
                else
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Item not found for update.", item.DataAndIdentifiers()));
                }
            }

            return await Task.FromResult(response);
        }

        public override async Task<DbValueCollectionOperationResponse<List<DbItem>>> DeleteItems(string tableName, List<DbItem> items)
        {
            var response = new DbValueCollectionOperationResponse<List<DbItem>>();

            foreach (var item in items)
            {
                var existingItem = _inMemoryData.FirstOrDefault(existing => existing.Identifiers.SequenceEqual(item.Identifiers));
                if (existingItem != null)
                {
                    _inMemoryData.Remove(existingItem);
                    response.ResponseValue.Add(existingItem);
                }
                else
                {
                    response.OperationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Item not found for deletion.", item.DataAndIdentifiers()));
                }
            }

            return await Task.FromResult(response);
        }

        public override async Task<bool> TestConnection()
        {
            return await Task.FromResult(true);
        }
    }
}
