using System.ComponentModel;
using System.Text.Json.Serialization;


namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbField
    {
        public string Name { get; set; }

        //InternalName is the name of the field in the Collection. Used when more than one field has the same name but different types. 
        //todo: Consider. DbField.InternalName - should we use a bool when multiple fields have the same name but different types?
        //Problem: We need to match a Dict<string,object> Values dictionary.
        /// <summary>
        /// InternalName is the name of the field in the Collection. Used when more than one field has the same name but different types. 
        /// </summary>
        public string InternalName { get; set; } = null;

        [Description("FieldTypeType is supposed to be an enum of type SqlDbType, EdmType or TypeCode")]
        [JsonConverter(typeof(FieldTypeConverter))]
        public Enum FieldType { get; set; }

        //Length is only relevant for varchar or types with limited length. For sql types this is: 
        //geography, hierarchyid, nchar, nvarchar, varbinary, varchar, xml//CHARACTER_MAXIMUM_LENGTH NVARCHAR(MAX) is -1. This is 2 Gbyte of storage
        //if CHARACTER_MAXIMUM_LENGTH is -1, this means MAX. This is 2 Gbyte of storage
        public int? Length { get; set; }


        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// This value comes from the target database default value. E.g. '(newid())'
        /// </summary>
        public string TargetDbColumnDefault { get; set; }

        /// <summary>
        /// When column has IDENTITY (autonumbering) enabled, and values cannot be inserted or updated
        /// Trying to insert identity field anyway gives error: Cannot insert explicit value for identity column in table 'TableName' when IDENTITY_INSERT is set to OFF
        /// </summary>
        public bool IsIdentity { get; set; }

        public DbField()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Field name</param>
        /// <param name="fieldType">FieldTypeType is supposed to be an enum of type SqlDbType, EdmType or TypeCode</param>
        public DbField(string name, Enum fieldType)
        {
            Name = name;
            FieldType = fieldType;
        }


    }
}