namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
{
    public class DictionaryComparer : IEqualityComparer<Dictionary<string, object>>
    {
        readonly List<string> _keys;

        public DictionaryComparer(List<string> keys)
        {
            _keys = keys;
        }

        public bool Equals(Dictionary<string, object> dictionary1, Dictionary<string, object> dictionary2)
        {
            // Check if both dictionaries are null
            if (dictionary1 == null && dictionary2 == null)
            {
                return true;
            }
            // Check if either dictionary is null
            if (dictionary1 == null || dictionary2 == null)
            {
                return false;
            }

            // Filter the dictionaries based on the specified _keys
            var filteredDictionary1 = dictionary1.Where(pair => _keys.Contains(pair.Key)).ToList();
            var filteredDictionary2 = dictionary2.Where(pair => _keys.Contains(pair.Key)).ToList();

            // Check if the filtered dictionaries have the same count and contain the same key-value pairs
            var areFilteredDictionariesEqual = filteredDictionary1.Count == filteredDictionary2.Count && !filteredDictionary1.Except(filteredDictionary2).Any();

            //This method compares only on the _keys of the dictionaries. But to compare the entire dictionaries, use the below code
            //var areOriginalDictionariesEqual = dictionary1.Count == dictionary2.Count && filteredDictionary1.Count == dictionary1.Count && filteredDictionary2.Count == dictionary2.Count && !dictionary1.Except(dictionary2).Any();

            return areFilteredDictionariesEqual;
        }


        public int GetHashCode(Dictionary<string, object> obj)
        {
            int hash = 17;
            //Compute a hash code based on the key-value pairs in the dictionary
            //foreach (var pair in obj.OrderBy(p => p.Key))
            //{
            //    hash = hash * 23 + pair.Key.GetHashCode();
            //    hash = hash * 23 + (pair.Value?.GetHashCode() ?? 0);
            //}

            foreach (var key in obj.Keys.OrderBy(k => k))
            {
                hash = hash * 23 + key.GetHashCode();
            }
            return hash;
        }
    }


}
