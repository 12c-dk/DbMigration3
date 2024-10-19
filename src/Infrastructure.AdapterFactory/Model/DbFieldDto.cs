using System.ComponentModel;
using System.Text.Json.Serialization;
using Infrastructure.AdapterFactory.Converter;

namespace Infrastructure.AdapterFactory.Model
{
    public class DbFieldDto
    {
        public string Name { get; set; }

        public string InternalName { get; set; }

        [Description("FieldTypeType is supposed to be an enum of type SqlDbType, EdmType or TypeCode")]
        [JsonConverter(typeof(FieldTypeConverter))]
        public Enum FieldType { get; set; }

        public int? Length { get; set; }

        public bool IsPrimaryKey { get; set; }

        public string TargetDbColumnDefault { get; set; }

        public bool IsIdentity { get; set; }

        public DbFieldDto() { }

        public DbFieldDto(string name, Enum fieldType)
        {
            Name = name;
            FieldType = fieldType;
        }
    }
}