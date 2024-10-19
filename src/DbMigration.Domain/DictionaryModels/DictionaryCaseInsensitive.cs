namespace DbMigration.Domain.DictionaryModels
{
    /// <summary>
    /// Dictionary with case in-sensitive keys
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DictionaryCaseInsensitive<T> : Dictionary<string, T>
    {
        public DictionaryCaseInsensitive() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public DictionaryCaseInsensitive(IEnumerable<KeyValuePair<string, T>> dictionary) : base(StringComparer.OrdinalIgnoreCase)
        {
            foreach (KeyValuePair<string, T> item in dictionary)
            {
                Add(item.Key, item.Value);
            }
        }
    }

}
