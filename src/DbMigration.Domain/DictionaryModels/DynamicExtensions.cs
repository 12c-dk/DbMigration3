using DbMigration.Domain.Model;

namespace DbMigration.Domain.DictionaryModels
{
    public static class DynamicExtensions
    {
        /// <summary>
        /// This cannot be used with List<DbItem>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="identifierKeys"></param>
        /// <returns></returns>
        public static List<DbItem> ToDbItemsWithIdentifiers(this IEnumerable<dynamic> data, IEnumerable<string> identifierKeys = null)
        {
            // Prevent the method from being called with DbItem
            if (data.Any() && data.First().GetType() == typeof(DbItem))
            {
                throw new InvalidOperationException("This method cannot be called with DbItem type.");
            }

            // Ensure case-insensitive comparison
            var identifierKeySet = identifierKeys != null
                ? new HashSet<string>(identifierKeys, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var outputData = new List<DbItem>();

            foreach (var row in data)
            {
                DbItem dbItem = new DbItem();

                foreach (dynamic prop in row)
                {
                    var key = prop.Key;
                    var value = prop.Value;

                    if (identifierKeySet.Contains(key))
                    {
                        dbItem.Identifiers.Add(key, value);
                    }
                    else
                    {
                        dbItem.Data.Add(key, value);
                    }
                }

                outputData.Add(dbItem);
            }

            return outputData;
        }

        public static List<Dictionary<string, object>> DynamicToDictionary(this IEnumerable<dynamic> data)
        {
            var outputList = new List<Dictionary<string, object>>();

            foreach (var row in data)
            {
                var itemValues = new Dictionary<string, object>();

                foreach (var prop in row)
                {
                    itemValues.Add(prop.Key, prop.Value);
                }

                outputList.Add(itemValues);
            }

            return outputList;
        }

        /// <summary>
        /// Splits DbItem Data into Identifiers and Data
        /// </summary>
        /// <param name="dbItemList"></param>
        /// <param name="identifierKeys"></param>
        /// <returns></returns>
        public static List<DbItem> DataToDbItemsWithIdentifiers(this List<DbItem> dbItemList, string[] identifierKeys = null)
        {
            // Ensure case-insensitive comparison
            var identifierKeySet = identifierKeys != null
                ? new HashSet<string>(identifierKeys, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in dbItemList)
            {
                var keysToRemove = new List<string>();

                foreach (var prop in item.Data)
                {
                    var key = prop.Key;
                    var value = prop.Value;

                    if (identifierKeySet.Contains(key))
                    {
                        // Check if the key already exists in Identifiers
                        if (item.Identifiers.ContainsKey(key))
                        {
                            throw new InvalidOperationException($"The key '{key}' exists in both Data and Identifiers.");
                        }

                        // Move the key-value pair to Identifiers
                        item.Identifiers.Add(key, value);
                        keysToRemove.Add(key);
                    }
                }

                // Remove the keys from Data after iteration
                foreach (var key in keysToRemove)
                {
                    item.Data.Remove(key);
                }
            }

            return dbItemList;
        }
    }
}
