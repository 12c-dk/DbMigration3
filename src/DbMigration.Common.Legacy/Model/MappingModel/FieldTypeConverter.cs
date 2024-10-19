using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.MappingModel;

/// <summary>
/// Used for converting a field from Json to the correct Enum type. 
/// Used as decorator: [JsonConverter(typeof(FieldTypeConverter))]
/// Used on: DbField
/// </summary>
public class FieldTypeConverter : JsonConverter<Enum>
{
    public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonString = reader.GetString() ?? throw new ArgumentNullException(nameof(reader));

        var splitString = jsonString.Split(".");
        if (splitString.Length != 2)
        {
            throw new ArgumentException($"The string '{reader.GetString()}' doesn't contain the expected Type and value format (e.g. 'SqlDbType.VarChar').");
        }

        if (splitString[0] == nameof(SqlDbType))
        {
            if (Enum.TryParse(splitString[1], out SqlDbType outType))
            {
                return outType;
            }
            else
            {
                throw new ArgumentException($"The string '{reader.GetString()}' could not be converted to an SqlDbType.");
            }
        }
        else if (splitString[0] == nameof(EdmType))
        {
            if (Enum.TryParse(splitString[1], out EdmType outType))
            {
                return outType;
            }
            else
            {
                throw new ArgumentException($"The string '{reader.GetString()}' could not be converted to an EdmType.");
            }
        }
        else if (splitString[0] == nameof(TypeCode))
        {
            if (Enum.TryParse(splitString[1], out TypeCode outType))
            {
                return outType;
            }
            else
            {
                throw new ArgumentException($"The string '{reader.GetString()}' could not be converted to an TypeCode.");
            }
        }
        else
        {
            throw new NotSupportedException("The specified type is not supported.");
        }
    }

    public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
    {
        if (value.GetType() == typeof(SqlDbType))
        {
            JsonSerializer.Serialize(writer, $"{nameof(SqlDbType)}.{value}", options);
        }
        else if (value.GetType() == typeof(EdmType))
        {
            JsonSerializer.Serialize(writer, $"{nameof(EdmType)}.{value}", options);
        }
        else if (value.GetType() == typeof(TypeCode))
        {
            JsonSerializer.Serialize(writer, $"{nameof(TypeCode)}.{value}", options);
        }
        else
        {
            throw new NotSupportedException("The specified type is not supported.");
        }

    }
}