using DbMigration.Common.Legacy.Model.MappingModel;

namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
{
    public class GenericRow : DictionaryCaseInsensitive<object>
    {
        public GenericRow(Dictionary<string, object> dictionary = null)
        {
            if (dictionary != null)
            {
                this.AddRangeOverride(dictionary);

            }

        }

        public DbItem ToDbItem()
        {
            return new DbItem(this);
        }
    }
}
