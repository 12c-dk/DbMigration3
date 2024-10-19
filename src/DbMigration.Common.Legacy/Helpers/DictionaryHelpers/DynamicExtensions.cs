using DbMigration.Common.Legacy.Model.MappingModel;

namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
{
    public static class DynamicExtensions
    {
        public static List<DbItem> ToDbItems(this IEnumerable<dynamic> data)
        {
            List<DbItem> outputData = new List<DbItem>();
            foreach (var row in data)
            {
                DbItem dbItem = new DbItem();

                foreach (var prop in row)
                {
                    dbItem.Add(prop.Key, prop.Value);
                }
                outputData.Add(dbItem);
            }

            return outputData;
        }

        public static List<Dictionary<string, object>> DynamicToDictionary(this IEnumerable<dynamic> data)
        {
            List<Dictionary<string, object>> outputList = new List<Dictionary<string, object>>();

            foreach (var row in data)
            {
                Dictionary<string, object> itemValues = new Dictionary<string, object>();

                foreach (var prop in row)
                {
                    itemValues.Add(prop.Key, prop.Value);
                }
                outputList.Add(itemValues);
            }

            return outputList;
        }

    }
}
