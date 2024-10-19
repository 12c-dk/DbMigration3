using System.Collections;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DbMigration.Common.Legacy.Helpers
{
    /// <summary>
    /// This class is used to customize the way Hashtable objects are serialized to and deserialized from JSON. It extends the JsonConverterFactory class, which is a factory class for converters that derive from JsonConverter&gt;T&lt;.
    /// </summary>
    public class JsonHashtableConverter : JsonConverterFactory
    {
        private static JsonConverter<Hashtable> _valueConverter;

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Hashtable);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            return _valueConverter ??= new HashtableConverterInner(options);
        }

        private class HashtableConverterInner : JsonConverter<Hashtable>
        {
            private readonly JsonSerializerOptions _options;

            JsonConverter<Hashtable> Converter => _valueConverter ??= (JsonConverter<Hashtable>)_options.GetConverter(typeof(Hashtable));

            public HashtableConverterInner(JsonSerializerOptions options)
            {
                _options = options;
            }

            public override Hashtable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                Hashtable hashtable = new Hashtable();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return hashtable;
                    }

                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string propertyName = reader.GetString();
                    reader.Read();

                    hashtable[propertyName ?? throw new InvalidOperationException("JsonHashtableConverter: reader.GetString() returned null. propertyName not available.")] = GetValue(ref reader, options);
                }
                return hashtable;
            }

            private object GetValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return reader.GetString();
                    case JsonTokenType.False:
                        return false;
                    case JsonTokenType.True:
                        return true;
                    case JsonTokenType.Null:
                        return null;
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out long longNumberValue))
                            return longNumberValue;
                        else if (reader.TryGetDecimal(out decimal decimalValue))
                            return decimalValue;
                        throw new JsonException("Unhandled Number value");
                    case JsonTokenType.StartObject:
                        return JsonSerializer.Deserialize<Hashtable>(ref reader, options);
                    case JsonTokenType.StartArray:
                        List<object> array = new List<object>();
                        while (reader.Read() &&
                            reader.TokenType != JsonTokenType.EndArray)
                        {
                            array.Add(GetValue(ref reader, options));
                        }
                        return array.ToArray();
                }
                throw new JsonException($"Unhandled TokenType {reader.TokenType}");
            }

            public override void Write(Utf8JsonWriter writer, Hashtable hashtable, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (KeyValuePair<string, object> kvp in hashtable)
                {
                    writer.WritePropertyName(kvp.Key);

                    if (Converter != null &&
                        kvp.Value is Hashtable value)
                    {
                        Converter.Write(writer, value, options);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, kvp.Value, options);
                    }
                }

                writer.WriteEndObject();
            }
        }
    }
}
