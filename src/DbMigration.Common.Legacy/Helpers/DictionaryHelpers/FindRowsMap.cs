using DbMigration.Common.Legacy.Model.MappingModel;

namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
{
    public class FindRowsMap
    {
        /// <summary>
        /// Get matching items from source and target tables based on compare keys
        /// </summary>
        /// <param name="source">Source List Dictionary for comparison</param>
        /// <param name="target">Target List Dictionary for comparison</param>
        /// <param name="compareKeys"></param>
        /// <returns>A tuple of a dictionary containing matched source items (Dictionary) and their matched target items (Dictionary), and a list of non-matched items </returns>
        /// <exception cref="Exception"></exception>
        public static DbValueMapOperationResponse<DbItem>
            FindMatchingRowMap( //Method name
                List<DbItem> source,
                List<DbItem> target,
                List<string> compareKeys)
        {
            var result = new DbValueMapOperationResponse<DbItem>();

            foreach (var sourceRow in source)
            {
                var multipleMatches = target.Where(targetRow => IsRowMatch(sourceRow, targetRow, compareKeys)).ToList();
                if (multipleMatches is {Count: > 1})
                {
                    var idValues = GetValuesByKeys(multipleMatches.First(), compareKeys);
                    throw new Exception($"Duplicate items found in target. Items are not unique by compare keys {string.Join(",", compareKeys)}. Values: {string.Join(",", idValues.Values)}");
                }

                var targetRowMatch = multipleMatches.SingleOrDefault();
                if (targetRowMatch != null)
                {
                    if (result.ResponseValue.ContainsValue(targetRowMatch))
                    {
                        var idValues = GetValuesByKeys(multipleMatches.First(), compareKeys);
                        throw new Exception($"Multiple matches found in source. Items are not unique by compare keys {string.Join(",", compareKeys)}. Values: {string.Join(",", idValues.Values)}");
                    }
                    result.ResponseValue.Add(sourceRow, targetRowMatch);
                }
                else
                {
                    result.NoMatchItems.Add(sourceRow);
                }
            }

            return result;
        }

        public static bool IsRowMatch(DictionaryCaseInsensitive<object> sourceRow, DictionaryCaseInsensitive<object> targetRow, List<string> keys)
        {
            var result = sourceRow.All(
                srcPair => !keys.Contains(srcPair.Key, StringComparer.OrdinalIgnoreCase)
                           || targetRow.ContainsKey(srcPair.Key)
                           && targetRow[srcPair.Key].Equals(srcPair.Value));

            return result;
        }

        public static DictionaryCaseInsensitive<object> GetValuesByKeys(DictionaryCaseInsensitive<object> dict, List<string> keys)
        {
            var result = new DictionaryCaseInsensitive<object>();
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out object value))
                {
                    result.Add(key, value);
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if source items and values are unique by compare keys
        /// </summary>
        /// <param name="source">A table of data</param>
        /// <param name="keys">A list of primary keys to use for evaluation</param>
        /// <returns></returns>
        public static bool CollectionKeysAreUnique(List<DictionaryCaseInsensitive<object>> source, List<string> keys)
        {
            IEnumerable<DictionaryCaseInsensitive<object>> keyValuesTable = source.Select(row => GetValuesByKeys(row, keys));

            var distinctObjects = keyValuesTable.Distinct(new EqualityComparerByJson<DictionaryCaseInsensitive<object>>());

            bool areUnique = distinctObjects.Count() == source.Count;
            return areUnique;

        }
    }

    public class DictionaryToLowerKeyComparer : IEqualityComparer<Dictionary<string, object>>
    {
        public bool Equals(Dictionary<string, object> x, Dictionary<string, object> y)
        {
            if (x == null || y == null)
                return false;

            return x.Keys.All(key => y.ContainsKey(key) && string.Equals(x[key].ToString()?.ToLower(), y[key].ToString()?.ToLower()));
        }

        public int GetHashCode(Dictionary<string, object> obj)
        {
            return obj.GetHashCode();
        }
    }
}
