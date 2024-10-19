using DbMigration.Common.Legacy.Helpers.DictionaryHelpers;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbItem : DictionaryCaseInsensitive<object>
    {
        public DbItem()
        {
        }

        public DbItem(Dictionary<string, object> dictionary = null)
        {
            if (dictionary != null)
            {
                this.AddRangeOverride(dictionary);
            }
        }

        public GenericRow ToGenericRow()
        {
            return new GenericRow(this);
        }


    }
}
