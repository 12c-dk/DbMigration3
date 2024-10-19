using DbMigration.Common.Legacy.Helpers.DictionaryHelpers;
using System.Text;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    /// <summary>
    /// A collection contains data for one table. 
    /// </summary>
    public class DbData : List<DbItem>
    {
        //A SchemaRepository will be used to get the schema

        public Guid SchemaId { get; set; }

        public DbData(Guid schemaId, List<DbItem> items = null)
        {
            SchemaId = schemaId;
            if (items != null)
            {
                AddRange(items);
            }
        }

        public int SchemaVersion { get; set; }

        public string ToString(List<string> columns = null, int top = 10, string delimiter = "\t")
        {
            StringBuilder sb = new StringBuilder();

            //If columns is undefined, set it to the first item's columns
            if (columns is null)
            {
                columns = new List<string>();
                var item = this[0];
                foreach (var value in item)
                {
                    if (!columns.Contains(value.Key))
                    {
                        columns.Add(value.Key);
                    }
                }
            }

            foreach (var column in columns)
            {
                sb.Append(column);
                sb.Append(delimiter);
            }
            sb.AppendLine();

            foreach (var item in this.Take(top))
            {
                foreach (var column in columns)
                {
                    if (item.ContainsKey(column))
                    {
                        if (item[column] == null)
                        {
                            sb.Append("NULL");
                        }
                        else
                        {
                            sb.Append(item[column]);
                        }
                    }
                    sb.Append(delimiter);
                }
                sb.AppendLine();
            }
            return sb.ToString();

        }

        public List<DictionaryCaseInsensitive<object>> ToListDictionaryCaseInsensitive()
        {
            return this.Select(dataItem =>
            {
                DictionaryCaseInsensitive<object> item = dataItem.ToDictionaryCaseInsensitive();

                return item;
            }).ToList();
        }
    }
}
