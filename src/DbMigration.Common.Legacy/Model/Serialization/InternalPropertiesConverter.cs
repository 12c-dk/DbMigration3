using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace DbMigration.Common.Legacy.Model.Serialization
{
    public class InternalPropertiesConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T instance = Activator.CreateInstance<T>();
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            foreach (var prop in properties)
            {
                if (document.RootElement.TryGetProperty(prop.Name, out JsonElement element))
                {
                    //element.GetProperty()
                    bool isJsonIgnore = prop.IsDefined(typeof(JsonIgnoreAttribute), true);

                    if (!isJsonIgnore)
                    {
                        object value = element.Deserialize(prop.PropertyType, options);
                        prop.SetValue(instance, value);
                    }
                }
            }

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            writer.WriteStartObject();
            foreach (var prop in properties)
            {
                bool isJsonIgnore = prop.IsDefined(typeof(JsonIgnoreAttribute), true);
                if (!isJsonIgnore)
                {
                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, prop.GetValue(value), prop.PropertyType, options);
                }

            }
            writer.WriteEndObject();
        }
    }


}
