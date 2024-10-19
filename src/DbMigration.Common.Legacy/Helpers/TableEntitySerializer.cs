using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DbMigration.Common.Legacy.Helpers
{
    public class TableEntitySerializer
    {

        public static string Serialize(List<DynamicTableEntity> entities)
        {
            List<JObject> jsonArray = new List<JObject>();
            foreach (DynamicTableEntity dynamicTableEntity in entities)
            {
                JObject jsonObject = new JObject();
                jsonObject["PartitionKey"] = dynamicTableEntity.PartitionKey;
                jsonObject["RowKey"] = dynamicTableEntity.RowKey;
                jsonObject["Timestamp"] = dynamicTableEntity.Timestamp;

                foreach (var property in dynamicTableEntity.Properties)
                {
                    jsonObject[property.Key] = JToken.FromObject(property.Value.PropertyAsObject);
                }

                jsonArray.Add(jsonObject);
            }

            string result = JsonConvert.SerializeObject(jsonArray);
            return result;
        }

        public static List<DynamicTableEntity> Deserialize(string jsonString)
        {
            List<DynamicTableEntity> outputList = new List<DynamicTableEntity>();

            JArray jArray = JArray.Parse(jsonString);
            foreach (var obj in jArray)
            {
                JObject jsonObject = (JObject)obj;
                string partitionKey = jsonObject["PartitionKey"].ToString();
                string rowKey = jsonObject["RowKey"].ToString();
                DateTimeOffset timestamp = jsonObject["Timestamp"].ToObject<DateTimeOffset>();

                DynamicTableEntity entity = new DynamicTableEntity(partitionKey, rowKey)
                {
                    Timestamp = timestamp
                };

                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name != "PartitionKey" && property.Name != "RowKey" && property.Name != "Timestamp")
                    {
                        entity.Properties[property.Name] = new EntityProperty(property.Value.ToString());
                    }
                }

                outputList.Add(entity);
            }
            return outputList;
        }

    }
}
