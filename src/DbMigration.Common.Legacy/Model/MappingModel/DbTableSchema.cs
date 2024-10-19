using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    /// <summary>
    /// Schema of a table
    /// </summary>
    public class DbTableSchema
    {
        public string TableName { get; set; }
        public Guid SchemaId { get; set; }
        public string DatabaseName { get; set; }

        private DbDatabase _parentDbDatabase;

        public DbTableSchemaConfig Config { get; set; } = new DbTableSchemaConfig();


        [JsonIgnore]
        public DbDatabase ParentDbDatabase
        {
            get => _parentDbDatabase;
            set
            {
                //During EnsureDatabase Tables are populated from deserialization. So Database already has this table. 
                //Here we are setting the reference back to the database. 
                //Consider if this parent/child relationship should be configured differently
                _parentDbDatabase = value;
                DatabaseName = value.DatabaseName;
            }
        }

        //fields should not be used directly. Need to validate when adding. But it needs to be a public property for serialization
        List<DbField> _fields = new List<DbField>();

        internal List<DbField> Fields { get => _fields; set => _fields = value; }

        [JsonIgnore]
        public IReadOnlyList<DbField> FieldsReadOnly => Fields;


        //Required for deserializing. Whenever deserializing, manually ensure that ParentDbDatabase is set
        public DbTableSchema()
        {
        }

        public DbTableSchema(Guid schemaId, DbDatabase database)
        {
            SchemaId = schemaId;
            ParentDbDatabase = database;
            DatabaseName = database.DatabaseName;
        }

        public void Validate()
        {
            //Todo: Rewrite to return operationresponse
            if (SchemaId == Guid.Empty)
            {
                throw new ApplicationException("Schema validation failed. SchemaId is not set");
            }

            if (ParentDbDatabase == null)
            {
                throw new ApplicationException("Schema validation failed. ParentDatabase is not set");
            }

            if (string.IsNullOrEmpty(TableName))
            {
                throw new ApplicationException("Schema validation failed. TableName is not set");
            }

        }

        /// <summary>
        /// Returns DbValueOperationResponse.ResponseValue with dictionary of Identifiers if ok. If error returns operationResponse;
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public DbValueOperationResponse<Dictionary<string, object>> EnsurePrimaryKeys(DbItem item)
        {
            //Todo: Modify this to handle a list of DbItem
            //Returns error if identifier fields are missing, if schema don't have identifier fields.
            DbValueOperationResponse<Dictionary<string, object>> response = new DbValueOperationResponse<Dictionary<string, object>>();

            DbOperationResponse operationResponse = new DbOperationResponse();

            if (!item.Values.Any())
            {
                operationResponse.ItemResponses.Add(new DbItemResponse(
                    DbOperationResponseSeverity.Error,
                    "Item has no values to be set. ",
                    new Dictionary<string, object>()));
                response.OperationResponse = operationResponse;
                return response;
            }
            //This is used from insert operation. Insert doesn't require identifiers, but ItemHasPrimaryKeys returns error if identifiers are missing
            bool primaryKeysSet = ItemHasPrimaryKeys(item);
            if (!primaryKeysSet)
            {
                operationResponse.ItemResponses.Add(new DbItemResponse(
                    DbOperationResponseSeverity.Error,
                    "One or more PrimaryKey values are missing.",
                    item));
                response.OperationResponse = operationResponse;
                return response;
            }

            //IdentifiersSet has been checked, e.g. all schema Identifier fields are present in item. But check if schema has no Identifier fields
            var identifiers = GetItemPrimaryKeys(item);
            if (!identifiers.Any())
            {
                //Error: identifiers not set in schema
                operationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error, "Schema doesn't have Identifier fields. Cannot upsert item.", item));
                response.OperationResponse = operationResponse;
                return response;
            }

            response.ResponseValue = identifiers;

            return response;

        }

        /// <summary>
        /// Separates input items that has PrimaryKeys and those that don't. Returns a list of items that has primary keys and a list of items that doesn't have primary keys
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public (List<DbItem>, List<DbItem>) GetItemsWithPrimaryKeys(List<DbItem> items)
        {
            List<DbItem> itemsWithKeys = new List<DbItem>();
            List<DbItem> itemsWithoutKeys = new List<DbItem>();

            foreach (var item in items)
            {
                bool hasPrimaryKeys = ItemHasPrimaryKeys(item);
                if (hasPrimaryKeys)
                {
                    itemsWithKeys.Add(item);
                }
                else
                {
                    itemsWithoutKeys.Add(item);
                }

            }
            return (itemsWithKeys, itemsWithoutKeys);
        }

        public (List<DbItem>, List<DbItem>) GetItemsWithRequiredInsertFields(List<DbItem> items)
        {
            List<DbItem> itemsWithRequiredInsertFields = new List<DbItem>();
            List<DbItem> itemsWithoutRequiredInsertFields = new List<DbItem>();

            foreach (var item in items)
            {
                bool hasRequiredInsertFields = ItemHasRequiredInsertFields(item);

                if (hasRequiredInsertFields)
                {
                    itemsWithRequiredInsertFields.Add(item);
                }
                else
                {
                    itemsWithoutRequiredInsertFields.Add(item);
                }

            }
            return (itemsWithRequiredInsertFields, itemsWithoutRequiredInsertFields);
        }



        /// <summary>
        /// Returns DbValueOperationResponse.ResponseValue with dictionary of Identifiers if ok. If error returns OperationResponse.ItemResponses
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public DbOperationResponse ValidateForUpsert(DbItem item)
        {
            //Todo: Rewrite to batch operation

            DbOperationResponse response = new DbOperationResponse();

            var identifierOperationResponse = EnsurePrimaryKeys(item);
            if (identifierOperationResponse.OperationResponse.ItemStatus == DbOperationResponseSeverity.Error)
            {
                response.ItemResponses.AddRange(identifierOperationResponse.OperationResponse.ItemResponses);
                return response;
            }

            return response;

        }

        //ValidateItem checks ID field. This should only be done on existing fields. New fields for insert might not have primary keys set in case of autonumberin
        public DbOperationResponse ValidateItem(DbItem item)
        {
            //Check primary keys
            DbOperationResponse operationResponse = new DbOperationResponse();
            var primaryKeys = FieldsReadOnly.Where(f => f.IsPrimaryKey).ToList();
            if (!primaryKeys.Any())
            {
                //Todo: Add option to config to ignore primary keys
                operationResponse.GeneralResponses.Add(new GeneralError(DbOperationResponseSeverity.Error, "Schema doesn't have primary keys defined"));
                return operationResponse;
            }
            foreach (DbField primaryKey in primaryKeys)
            {
                if (!item.Any(v => v.Key.ToLower() == primaryKey.Name.ToLower()))
                {
                    operationResponse.ItemResponses.Add(new DbItemResponse(DbOperationResponseSeverity.Error,
                        $"Item is missing primary key {primaryKey.Name}", item));
                }
            }

            //Todo: Add checks on fields, like if string field is longer than length

            return operationResponse;
        }

        /// <summary>
        /// Returns true if all schema keys are present in item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool ItemHasPrimaryKeys(DbItem item)
        {
            var schemaPrimaryKeys = FieldsReadOnly.Where(f => f.IsPrimaryKey).ToList();
            if (schemaPrimaryKeys.Count == 0)
            {
                throw new ApplicationException("ItemHasPrimaryKeys called on schema without primary keys");
            }
            bool allSchemaKeysPresentInItem = schemaPrimaryKeys.All(sk => item.Any(iv => iv.Key.ToLower() == sk.Name.ToLower()));
            return allSchemaKeysPresentInItem;
        }

        public bool ItemHasIdentityFields(DbItem item)
        {
            var schemaIdentityKeys = FieldsReadOnly.Where(f => f.IsIdentity).ToList();
            if (schemaIdentityKeys.Count == 0)
            {
                throw new ApplicationException("ItemHasIdentityFields called on schema without primary keys");
            }
            bool allSchemaKeysPresentInItem = schemaIdentityKeys.All(sk => item.Any(iv => iv.Key.ToLower() == sk.Name.ToLower()));
            return allSchemaKeysPresentInItem;
        }

        public bool ItemHasRequiredInsertFields(DbItem item)
        {
            throw new NotImplementedException();
            //Todo: Need the not null flag on fields. 
            //var schemaPrimaryKeys = FieldsReadOnly.Where(f => f.IsPrimaryKey).ToList();
            //bool allSchemaKeysPresentInItem = schemaPrimaryKeys.All(sk => item.Any(iv => iv.Key.ToLower() == sk.Name.ToLower()));
            //return allSchemaKeysPresentInItem;
        }

        //GetItemUpsertFields - Not including identifiers. 

        /// <summary>
        /// Get all fields that match schema fields, except for identifiers. Non-matching fields may be included based on configuration OnInsertFieldsNotInSchemaResponse. Includes PrimaryKeys, but not identifiers.
        /// Should it be called: RemoveNonSchemaFields?
        /// </summary>
        /// <param name="item"></param>
        public DbValueOperationResponse<Dictionary<string, object>> GetItemValuesMatchingSchemaFields(DbItem item)
        {
            //todo: This operation name is very confusing. Clear up purpose or remove. 

            DbValueOperationResponse<Dictionary<string, object>> response = new DbValueOperationResponse<Dictionary<string, object>>();

            //Schema field IsIdentity indicates fields that doesn't take input. 
            //fieldsMatchingSchemaNonIdentifiers may contain IsPrimarykey fields. 
            //Get all fields that match schema fields, except for identifiers
            List<KeyValuePair<string, object>> fieldsMatchingSchemaNonIdentifiers = item.Where(dbItem =>
                FieldsReadOnly.Any(schemaField =>
                    dbItem.Key.ToLower() == schemaField.Name.ToLower() && !schemaField.IsIdentity)).ToList();
            //If identifiers are included in DbItem we add them to the insert fields.
            //Consider: id field matching. Coming from a source that doesn't have the id's.
            //or source that has id fields to be pushed

            //What if identifiers are required in insert? If field is not a required field? 
            //Check on the null property? Can check for default value in sql?
            //How about - if provided, include in insert?

            //How about: If all identifier fields are supplied try upsert. Otherwise insert. 
            //How about id field write-back?
            //If id fields are not available, can we match on all values? -
            //No, On a second run values might have changed and duplicate records will exist. 
            //For consecutive runs we need a unique identifier. 

            //For upsert, we need id's
            //for Insert, we don't need id's


            if (ItemHasFieldsNotInSchema(item))
            {

                switch (Config.OnInsertFieldsNotInSchemaResponse)
                {
                    case DbTableSchemaReaction.Ignore: //nonMatchingFields should not be included in upsert
                        response.ResponseValue = fieldsMatchingSchemaNonIdentifiers.ToDictionary(k => k.Key, v => v.Value);
                        return response;
                    case DbTableSchemaReaction.Error: //Insert should be aborted
                                                      //response.OperationResponse = new DbOperationResponse();
                                                      //Method? GetDbItemFieldsInSchema


                        var primaryKeysResponse = EnsurePrimaryKeys(item);
                        Dictionary<string, object> operationResponseItemReference = primaryKeysResponse.ResponseValue ??
                            item;

                        response.OperationResponse.ItemResponses.Add(
                            new DbItemResponse(
                                DbOperationResponseSeverity.Error,
                                $"Item contains fields that are not in schema: {string.Join(",", GetFieldsNotInSchema(item).Select(f => f.Key))}",
                                operationResponseItemReference));
                        return response;
                    case DbTableSchemaReaction.Include: //nonMatchingFields should be included in upsert

                        fieldsMatchingSchemaNonIdentifiers.AddRange(GetFieldsNotInSchema(item));

                        var matchingFieldsDictionary = fieldsMatchingSchemaNonIdentifiers.ToDictionary(k => k.Key, v => v.Value);

                        response.ResponseValue = matchingFieldsDictionary;
                        return response;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            else
            {
                response.ResponseValue = fieldsMatchingSchemaNonIdentifiers.ToDictionary(k => k.Key, v => v.Value);
                return response;
            }

        }

        public Dictionary<string, object> GetItemPrimaryKeys(DbItem item)
        {
            IEnumerable<DbField> schemaPrimaryKeys = FieldsReadOnly.Where(f => f.IsPrimaryKey);
            IEnumerable<KeyValuePair<string, object>> itemKeyFields = item.Where(ipk =>
                schemaPrimaryKeys.Any(spk =>
                    spk.Name.ToLower() == ipk.Key.ToLower()));
            Dictionary<string, object> keyFieldsDict = itemKeyFields.ToDictionary(k => k.Key, v => v.Value);
            return keyFieldsDict;
        }



        public DbField GetField(string fieldName, Enum fieldType)
        {
            DbField existingField = _fields.FirstOrDefault(f => f.Name.ToLower() == fieldName.ToLower() && f.FieldType.Equals(fieldType));
            return existingField;

        }

        public DbField GetFieldNameExists(string fieldName)
        {
            DbField existingField = _fields.FirstOrDefault(f => f.Name.ToLower() == fieldName.ToLower());
            return existingField;
        }

        public DbField EnsureTableField(string fieldName, Enum fieldType)
        {
            DbField existingField = GetField(fieldName, fieldType);

            if (existingField != null)
            {
                return existingField;
            }

            //ParentDbDatabase might be null, if class is instanciated in a different context
            if (!Config.TableAllowDuplicateNames)
            {
                var existingFieldWithSameName = GetFieldNameExists(fieldName);

                if (existingFieldWithSameName != null)
                {
                    throw new ApplicationException(
                        $"Trying to ensure field {fieldName} in table {TableName} with type {fieldType} but it already exists with type {existingFieldWithSameName.FieldType}");
                }

            }

            DbField field = new DbField(fieldName, fieldType);
            Fields.Add(field);
            return field;

        }

        //Ensures that field exists by name and fieldType, but doesn't update properties. 
        public DbField EnsureTableField(DbField newOrUpdatedField)
        {
            DbField existingField = GetField(newOrUpdatedField.Name, newOrUpdatedField.FieldType);

            if (existingField != null)
            {
                return existingField;
            }


            if (Config?.TableAllowDuplicateNames == null || Config.TableAllowDuplicateNames)
            {
                var existingFieldWithSameName = GetFieldNameExists(newOrUpdatedField.Name);

                if (existingFieldWithSameName != null)
                {
                    throw new ApplicationException(
                        $"Trying to ensure field {newOrUpdatedField.Name} in table {TableName} with type {newOrUpdatedField.FieldType} but it already exists with type {existingFieldWithSameName.FieldType}");
                }
            }

            Fields.Add(newOrUpdatedField);
            return newOrUpdatedField;

        }


        public void LoadFromDbData(DbData data)
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (var item in data)
            {
                foreach (string key in item.Keys)
                {
                    var value = item[key];
                    var fieldType = GetFieldType(value);

                    var existingField = _fields.FirstOrDefault(f => f.Name == key && f.FieldType.Equals(fieldType));
                    if (existingField == null)
                    {
                        _fields.Add(new DbField(key, fieldType));
                    }
                }
            }
        }

        private Enum GetFieldType(object value)
        {
            //System.Enum of SqlDbType, EdmType or TypeCode

            //Todo: Handle also System.Enum of SqlDbType and EdmType 
            if (value == null)
            {
                return TypeCode.Empty;
            }

            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            return typeCode;
        }

        public bool ItemHasFieldsNotInSchema(DbItem item)
        {
            //Key item.Values.Keys that are not found in FieldsReadOnly
            bool itemHasFieldsNotInSchema = item.Keys.Any(key => FieldsReadOnly.All(f => f.Name != key));
            return itemHasFieldsNotInSchema;

        }

        public List<DbField> GetFieldsFromItem(DbItem item)
        {
            //Get fields from item.Values.Keys
            List<DbField> itemFields2 = item.Keys
                .Select(key => FieldsReadOnly.FirstOrDefault(f => f.Name == key))
                .Where(field => field != null)
                .ToList();

            return itemFields2;
        }

        public List<KeyValuePair<string, object>> GetFieldsNotInSchema(DbItem item)
        {
            List<KeyValuePair<string, object>> fieldsNotMatchingSchema =
                item.Where(itemValue =>
                    FieldsReadOnly.All(schemaFields => //Checks if all items matches condition - bool
                        itemValue.Key.ToLower() != schemaFields.Name.ToLower()
                    )
                ).ToList();
            return fieldsNotMatchingSchema;
        }

    }



    public class DbTableSchemaJsonConverter : JsonConverter<DbTableSchema>
    {

        public override DbTableSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implement custom deserialization if necessary. 
            // Using reflection because not all fields are public, especially the Fields collection are not supposed to be public
            var instance =
                Activator.CreateInstance(typeof(DbTableSchema), BindingFlags.Instance | BindingFlags.Public, null, null,
                    null) ?? throw new ApplicationException(
                    "Argument exception. Activator.CreateInstance(typeof(DbTableSchema), BindingFlags.Instance | BindingFlags.Public, null, null, null)");


            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "SchemaId":
                            //Getting SchemaId of the DbTableSchema model shouldn't be able to return null.
                            // ReSharper disable once NotResolvedInText
                            PropertyInfo schemaId = instance.GetType().GetProperty("SchemaId") ?? throw new ArgumentNullException("instance.GetType().GetProperty(\"SchemaId\")");
                            Guid schemaIdValue = reader.GetGuid();
                            schemaId.SetValue(instance, schemaIdValue);
                            break;
                        case "Fields":
                            string internalValue = reader.GetString();
                            List<DbField> fields;
                            fields = internalValue == null ? new List<DbField>() : JsonSerializer.Deserialize<List<DbField>>(internalValue);
                            PropertyInfo internalProp =
                                instance.GetType().GetProperty("Fields",
                                    BindingFlags.NonPublic | BindingFlags.Instance) ??
                                throw new ApplicationException(
                                    "ArgumentNullException: instance.GetType().GetProperty(\"Fields\", BindingFlags.NonPublic | BindingFlags.Instance)");
                            internalProp.SetValue(instance, fields);
                            break;
                        // ReSharper disable once RedundantEmptySwitchSection
                        default:
                            //string publicValue = reader.GetString() ?? throw new ArgumentNullException(nameof(reader));
                            //PropertyInfo instanceProperty = instance.GetType().GetProperty(propertyName);
                            //var theType = instanceProperty.GetType();

                            //var deserialized = JsonSerializer.Deserialize (publicValue);
                            //instanceProperty.GetType()
                            // Handle properties in a standard way
                            // Add your code here
                            break;
                    }
                }
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    //DbTableSchema newSchema = new DbTableSchema(fields);
                    //var instance2 = Activator.CreateInstance(typeof(DbTableSchema), BindingFlags.Instance | BindingFlags.Public, null, fields, null);

                    return (DbTableSchema)instance;
                }
            }



            throw new JsonException("Error reading JSON.");

        }

        public override void Write(Utf8JsonWriter writer, DbTableSchema value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            //writer.WriteString("PublicProperty", value.PublicProperty.ToString());

            // Access the internal property via reflection
            var internalPropertyValue = value
                .GetType()
                .GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(value);
            //Here we need to serialize the internalPropertyValue
            string serializedInternalPropertyValue = JsonSerializer.Serialize(internalPropertyValue, options);
            writer.WriteString("Fields", serializedInternalPropertyValue);

            writer.WriteString("SchemaId", value.SchemaId.ToString());

            writer.WriteEndObject();
        }
    }


}
