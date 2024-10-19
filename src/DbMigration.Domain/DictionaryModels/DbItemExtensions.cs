using DbMigration.Domain.Model;

namespace DbMigration.Domain.DictionaryModels
{
    public static class DbItemExtensions
    {
        public static List<DictionaryCaseInsensitive<object>> FormatAsDataAndIdentifiers(this List<DbItem> dbItems)
        {
            var formattedOutput = new List<DictionaryCaseInsensitive<object>>();
            foreach (var dbItem in dbItems)
            {
                formattedOutput.Add(dbItem.DataAndIdentifiers());
            }
            return formattedOutput;
        }
    }
}
