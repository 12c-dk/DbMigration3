using System.Text.Json;
using DbMigration.Common.Legacy.Model.MappingModel;


//using Castle.Components.DictionaryAdapter;

namespace DbMigration.Common.Legacy.Helpers
{
    /// <summary>
    /// This class contains helper methods for System.Text.Json. JSON serialization and deserialization.
    /// </summary>
    public class JsonHelper
    {
        public static List<Dictionary<string, object>> JsonListToEnumerableList(string json)
        {

            IEnumerable<Dictionary<string, object>> loadedDynamic2 = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(json);

            if (loadedDynamic2 is null)
            {
                return null;
            }

            List<Dictionary<string, object>> outputList = new List<Dictionary<string, object>>();

            foreach (var item in loadedDynamic2)
            {
                Dictionary<string, object> itemRow = new Dictionary<string, object>();

                foreach (var itemKey in item.Keys)
                {
                    if (item[itemKey] is JsonElement jElement)
                    {
                        object columnValue = item[itemKey];
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (columnValue is null)
                        {
                            itemRow.Add(itemKey, null);
                            // ReSharper disable once HeuristicUnreachableCode
                            continue;
                        }

                        itemRow.Add(itemKey, GetValue(ref jElement));

                    }
                    else if (item[itemKey] is null)
                    {
                        itemRow.Add(itemKey, null);
                    }
                    else
                    {
                        //var unknownType = item[itemKey];
                        throw new ApplicationException("Item value were expected to be a JElement, but weren't");
                    }
                }
                outputList.Add(itemRow);
            }
            return outputList;
        }

        public static DbData JsonListToDbData(Guid dbDataSchemaId, string json)
        {
            DbData outputData = new DbData(dbDataSchemaId);

            IEnumerable<Dictionary<string, object>> loadedDynamic = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(json);
            if (loadedDynamic is null)
            {
                return null;
            }

            foreach (var item in loadedDynamic)
            {
                Dictionary<string, object> itemRow = new Dictionary<string, object>();

                foreach (var itemKey in item.Keys)
                {
                    if (item[itemKey] is JsonElement jElement)
                    {
                        object columnValue = item[itemKey];
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (columnValue is null)
                        {
                            itemRow.Add(itemKey, null);
                            // ReSharper disable once HeuristicUnreachableCode
                            continue;
                        }

                        itemRow.Add(itemKey, GetValue(ref jElement));

                    }
                    else if (item[itemKey] is null)
                    {
                        itemRow.Add(itemKey, null);
                    }
                    else
                    {
                        //var unknownType = item[itemKey];
                        throw new ApplicationException("Item value were expected to be a JElement, but weren't");
                    }


                }
                DbItem dbItem = new DbItem(itemRow);

                outputData.Add(dbItem);

            }
            return outputData;
        }


        private static object GetValue(ref JsonElement jElement)
        {
            switch (jElement.ValueKind)
            {
                case JsonValueKind.Undefined:
                    throw new JsonException($"Unhandled TokenType {jElement.ValueKind}");
                case JsonValueKind.Object:
                    //This is a Json object. E.g. {"Address":"MainStreet 2"}
                    Dictionary<string, object> objectList = new Dictionary<string, object>();
                    foreach (var subElement in jElement.EnumerateObject())
                    {
                        var subElementVar = subElement.Value;
                        objectList.Add(subElement.Name, GetValue(ref subElementVar));
                    }
                    return objectList;
                case JsonValueKind.Array:
                    //This is a Json array. E.g. ["One",2,{}]
                    List<object> arrayList = new List<object>();
                    foreach (JsonElement subElement in jElement.EnumerateArray())
                    {
                        var subElementVar = subElement;
                        arrayList.Add(GetValue(ref subElementVar));
                    }
                    return arrayList;
                case JsonValueKind.String:
                    return jElement.GetString();
                case JsonValueKind.Number:
                    if (jElement.TryGetInt64(out long longValue))
                    {
                        return longValue;
                    }
                    if (jElement.TryGetDecimal(out decimal decimalValue))
                    {
                        return decimalValue;
                    }
                    throw new JsonException("Unhandled Number value");
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException($"Unhandled TokenType {jElement.ValueKind}");
            }
        }

        //private static DbValue getDbValue(ref JsonElement jElement)
        //{
        //    DbValue outputValue = new DbValue();

        //    switch (jElement.ValueKind)
        //    {
        //        case JsonValueKind.Undefined:
        //            throw new JsonException($"Unhandled TokenType {jElement.ValueKind}");
        //        case JsonValueKind.Object:
        //            //This is a Json object. E.g. {"Address":"MainStreet 2"}
        //            Dictionary<string, object> objectList = new Dictionary<string, object>();
        //            List<DbValue> dbValueList = new List<DbValue>();

        //            foreach (var subElement in jElement.EnumerateObject())
        //            {
        //                var subElementVar = subElement.Value;
        //                objectList.Add(subElement.Name, getDbValue(ref subElementVar));

        //                dbValueList.Add(new DbValue(subElement.Name, getDbValue(ref subElementVar)));
        //            }
        //            outputValue.Type = JsonValueKind.Object;
        //            outputValue.Value = dbValueList;
        //            return outputValue;
        //        case JsonValueKind.Array:
        //            //This is a Json array. E.g. ["One",2,{}]
        //            List<object> arrayList = new List<object>();
        //            foreach (JsonElement subElement in jElement.EnumerateArray())
        //            {
        //                var subElementVar = subElement;
        //                arrayList.Add(getDbValue(ref subElementVar));
        //            }
        //            outputValue.Type = JsonValueKind.Array;
        //            outputValue.Value = arrayList;
        //            return outputValue;
        //        case JsonValueKind.String:
        //            outputValue.Value = jElement.GetString();
        //            outputValue.Type = JsonValueKind.String;
        //            return outputValue;
        //        case JsonValueKind.Number:
        //            if (jElement.TryGetInt64(out long _long))
        //            {
        //                outputValue.Value = _long;
        //                outputValue.Type = JsonValueKind.Number;
        //                return outputValue;
        //            }
        //            if (jElement.TryGetDecimal(out decimal _dec))
        //            {
        //                outputValue.Value = _dec;
        //                outputValue.Type = JsonValueKind.Number;
        //                return outputValue;
        //            }
        //            throw new JsonException($"Unhandled Number value");
        //        case JsonValueKind.True:
        //            outputValue.Value = true;
        //            outputValue.Type = JsonValueKind.True;
        //            return outputValue;
        //        case JsonValueKind.False:
        //            outputValue.Value = false;
        //            outputValue.Type = JsonValueKind.False;
        //            return outputValue;
        //        case JsonValueKind.Null:
        //            outputValue.Value = null;
        //            outputValue.Type = JsonValueKind.Null;
        //            return outputValue;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }
        //    throw new JsonException($"Unhandled TokenType {jElement.ValueKind}");
        //}



    }
}
