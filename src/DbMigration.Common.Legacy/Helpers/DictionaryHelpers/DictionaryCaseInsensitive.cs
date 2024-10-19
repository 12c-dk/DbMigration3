using DbMigration.Common.Legacy.Model.MappingModel;

namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
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

        public DictionaryCaseInsensitive(Dictionary<string, T> dictionary) : base(StringComparer.OrdinalIgnoreCase)
        {
            foreach (var item in dictionary)
            {
                Add(item.Key, item.Value);
            }
        }
    }

    public static class DictionaryCaseInsensitiveExtensions
    {
        public static DbData ToDbData(this List<DictionaryCaseInsensitive<object>> listDictionary, Guid? schemaId = null)
        {
            schemaId ??= Guid.Empty;
            var dbData = new DbData((Guid)schemaId);

            foreach (var listItem in listDictionary)
            {
                DbItem dbItem = new DbItem(listItem);
                dbData.Add(dbItem);

            }

            return dbData;
        }

    }
}
