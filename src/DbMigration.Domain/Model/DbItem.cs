using DbMigration.Domain.DictionaryModels;

//Sync.Helpers.DictionaryHelpers;

namespace DbMigration.Domain.Model
{

    public class DbItem
    {
        public DictionaryCaseInsensitive<object> Data { get; set; } = new DictionaryCaseInsensitive<object>();

        /// <summary>
        /// Reason for having Data and Identifiers as separate dictionaries is for update scenarios. Items with a set of Identifiers needs one of the identifier fields updated. 
        /// </summary>
        public DictionaryCaseInsensitive<object> Identifiers { get; set; } = new DictionaryCaseInsensitive<object>();


        public DbItem(Dictionary<string, object> identifiers = null,
            Dictionary<string, object> data = null)
        {
            if (identifiers != null)
            {
                Identifiers.AddRangeOverride(identifiers);
            }

            if (data != null)
            {
                Data.AddRangeOverride(data);
            }
        }

        public DictionaryCaseInsensitive<object> DataToGenericRow()
        {
            return new DictionaryCaseInsensitive<object>(Data);
        }
        
        public DictionaryCaseInsensitive<object> IdentifiersToGenericRow()
        {
            return new DictionaryCaseInsensitive<object>(Identifiers);
        }

        public DictionaryCaseInsensitive<object> DataAndIdentifiers()
        {
            //Combine Identifiers and Data. If both have same Key, Identifier field is used. 
            //DictionaryCaseInsensitive<object> combined = new DictionaryCaseInsensitive<object>(Identifiers.Union(Data));

            DictionaryCaseInsensitive<object> dictionary = new DictionaryCaseInsensitive<object>();
            foreach (var identifier in Identifiers)
            {
                dictionary.Add(identifier.Key, identifier.Value);
            }

            // Add data to the dictionary only if the key doesn't exist in identifiers
            foreach (var data in Data)
            {
                if (!dictionary.ContainsKey(data.Key))
                {
                    dictionary.Add(data.Key, data.Value);
                }
            }

            return dictionary;
        }

    }
}