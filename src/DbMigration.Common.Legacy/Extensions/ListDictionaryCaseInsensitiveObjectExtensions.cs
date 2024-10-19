using DbMigration.Common.Legacy.Helpers.DictionaryHelpers;

namespace DbMigration.Common.Legacy.Extensions
{
    public static class ListDictionaryCaseInsensitiveObjectExtensions
    {
        /// <summary>
        /// Converts a list of DictionaryCaseInsensitive objects to a list of KeyValuePair arrays. Good for visualisation when debugger shows internal Dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, object>[]> ToKeyValueList(this List<DictionaryCaseInsensitive<object>> data)
        {
            List<KeyValuePair<string, object>[]> outputList = new List<KeyValuePair<string, object>[]>();

            foreach (DictionaryCaseInsensitive<object> item in data)
            {
                KeyValuePair<string, object>[] val2 = item.ToArray();
                outputList.Add(val2);
            }

            return outputList;
        }
    }
}
