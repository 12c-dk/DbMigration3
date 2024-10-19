using System.Text.Json;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.Serialization;

public class MsEntityPropertyConverter : System.Text.Json.Serialization.JsonConverter<EntityProperty>
{
    public override EntityProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Deserializing EntityProperty objects is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, EntityProperty value, JsonSerializerOptions options)
    {
        if (value.PropertyType == EdmType.Binary)
        {
            byte[] binaryValue = value.BinaryValue;
            writer.WriteStringValue(Convert.ToBase64String(binaryValue));
        }
        else
        {
            //writer.WriteObjectValue(value.PropertyAsObject);
            JsonSerializer.Serialize(writer, value.PropertyAsObject, options);
        }
    }
}