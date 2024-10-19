using System.Text;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    /// <summary>
    /// For returning success items (ResponseValue) and problem items (OperationResponse)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbValueOperationResponse<T> where T : new()
    {
        //Returns ResponseValue if operation is successful, otherwise returns OperationResponse
        public T ResponseValue { get; set; } = new T();
        public DbOperationResponse OperationResponse { get; set; } = new DbOperationResponse();

    }

    public class DbValueListOperationResponse<T> where T : new()
    {
        /// <summary>
        /// Dictionary of input items (Key) and output or matched items (Value)
        /// </summary>
        public List<T> ResponseValue { get; set; } = new List<T>();
        public List<T> NoMatchItems { get; set; } = new List<T>();

        public DbOperationResponse OperationResponse { get; set; } = new DbOperationResponse();

    }

    /// <summary>
    /// For returning input items mapped with success items (ResponseValue), Non-matched items (NoMatchItems) and problem items (OperationResponse)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbValueMapOperationResponse<T> where T : new()
    {
        /// <summary>
        /// Dictionary of input items (Key) and output or matched items (Value)
        /// </summary>
        public Dictionary<T, T> ResponseValue { get; set; } = new Dictionary<T, T>();
        //public List<T> NoMatchItems { get; set; } = new T();
        public List<T> NoMatchItems { get; set; } = new List<T>();

        public DbOperationResponse OperationResponse { get; set; } = new DbOperationResponse();

    }

    public class DbOperationResponse
    {
        public List<GeneralError> GeneralResponses { get; set; } = new List<GeneralError>();

        /// <summary>
        /// List of items where operation went wrong. Items contains only primary keys of failed items.
        /// </summary>
        public List<DbItemResponse> ItemResponses { get; set; } = new List<DbItemResponse>();

        /// <summary>
        /// List of items where operation went well. Items contains only primary keys of success items. 
        /// </summary>
        public List<DbItem> SuccessItems { get; set; } = new List<DbItem>();

        public DbOperationResponseSeverity GeneralStatus
        {
            get
            {
                if (GeneralResponses.Any(r => r.Severity == DbOperationResponseSeverity.Error))
                    return DbOperationResponseSeverity.Error;
                if (GeneralResponses.Any(r => r.Severity == DbOperationResponseSeverity.Warning))
                    return DbOperationResponseSeverity.Warning;
                return DbOperationResponseSeverity.Info;
            }
        }

        public DbOperationResponseSeverity ItemStatus
        {
            get
            {
                if (ItemResponses.Any(r => r.Severity == DbOperationResponseSeverity.Error))
                    return DbOperationResponseSeverity.Error;
                if (ItemResponses.Any(r => r.Severity == DbOperationResponseSeverity.Warning))
                    return DbOperationResponseSeverity.Warning;
                return DbOperationResponseSeverity.Info;
            }
        }


        public OperationResultEnum OperationResult
        {
            get
            {
                if (GeneralResponses.Any(r => r.Severity == DbOperationResponseSeverity.Error))
                {
                    return OperationResultEnum.Failure;
                }
                if (GeneralResponses.Any(r => r.Severity == DbOperationResponseSeverity.Warning))
                {
                    return OperationResultEnum.PartialSuccess;
                }

                if (ItemResponses.Any(r => r.Severity == DbOperationResponseSeverity.Error &&
                                           SuccessItems.Count > 0))
                {
                    return OperationResultEnum.PartialSuccess;
                }

                if (ItemResponses.Any(r => r.Severity == DbOperationResponseSeverity.Error &&
                                           SuccessItems.Count == 0))
                {
                    return OperationResultEnum.Failure;
                }

                if (ItemResponses.Any(r => r.Severity == DbOperationResponseSeverity.Warning))
                {
                    return OperationResultEnum.PartialSuccess;
                }

                return OperationResultEnum.Success;

            }
        }

        //Error object
        //    Levels: Table, row, field
        //    Data contained field that doesn't exist in schema. 
        //    Field contains data of a type that doesn't match the field type. 
        //    Field contains null, but field type is a non-nullable type
        //Ignored fields because not included in schema.
        //    Define response object that holds list of successful operations by identifier fields, unsuccessful operations by identifier fields with error message.

        //Enum: success, partialSuccess, failure

        //General errors (none of the operations can be executed because of a general problem)

        //ToString() - returns a string with the result with a high level description of the result

        public void Append(DbOperationResponse additionalResponse)
        {
            GeneralResponses.AddRange(additionalResponse.GeneralResponses);
            ItemResponses.AddRange(additionalResponse.ItemResponses);
            SuccessItems.AddRange(additionalResponse.SuccessItems);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Operation result: ");
            sb.Append(OperationResult.ToString());
            sb.AppendLine(". ");

            if (GeneralResponses.Count > 0)
            {
                sb.Append("General errors: ");
                sb.Append(GeneralResponses.Count);
                sb.Append(". ");
            }

            if (GeneralResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Error))
            {
                IEnumerable<GeneralError> generalErrors = GeneralResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Error);
                sb.Append("General errors: ");
                sb.Append(generalErrors.Count());
                sb.Append(". ");
            }

            if (GeneralResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Warning))
            {
                IEnumerable<GeneralError> generalErrors = GeneralResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Warning);
                sb.Append("General Warnings: ");
                sb.Append(generalErrors.Count());
                sb.Append(". ");
            }

            if (GeneralResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Info))
            {
                IEnumerable<GeneralError> generalErrors = GeneralResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Info);
                sb.Append("General Info responses: ");
                sb.Append(generalErrors.Count());
                sb.Append(". ");
            }


            if (ItemResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Error))
            {
                IEnumerable<DbItemResponse> itemErrors = ItemResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Error);
                sb.Append("Item errors: ");
                sb.Append(itemErrors.Count());
                sb.Append(". ");
            }

            if (ItemResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Warning))
            {
                IEnumerable<DbItemResponse> itemErrors = ItemResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Warning);
                sb.Append("Item warnings: ");
                sb.Append(itemErrors.Count());
                sb.Append(". ");
            }

            if (ItemResponses.Any(ir => ir.Severity == DbOperationResponseSeverity.Info))
            {
                IEnumerable<DbItemResponse> itemErrors = ItemResponses.Where(ir => ir.Severity == DbOperationResponseSeverity.Info);
                sb.Append("Item information responses: ");
                sb.Append(itemErrors.Count());
                sb.Append(". ");
            }

            sb.AppendLine();


            if (GeneralResponses.Count > 0)
            {
                IEnumerable<GeneralError> firstFiveGeneralErrors = GeneralResponses.OrderBy(r => r.Severity).Take(5);

                foreach (string s in firstFiveGeneralErrors.Select(ir => $"{ir.Severity}, OutputMessage: {ir.OutputMessage}"))
                {
                    sb.AppendLine(s);
                }
            }

            if (ItemResponses.Count > 0)
            {

                var firstFiveItemErrors = ItemResponses.OrderBy(i => i.Severity).Take(5);

                foreach (string s in firstFiveItemErrors.Select(ir => $"{ir.Severity}, Item primary keys: {ir.ItemIdentifiers} OutputMessage: {ir.OutputMessage}"))
                {
                    sb.AppendLine(s);
                }
            }


            return sb.ToString();

        }
    }

    public enum OperationResultEnum
    {
        Success,
        PartialSuccess,
        Failure
    }

    public enum DbOperationResponseSeverity
    {
        Error = 1,
        Warning = 2,
        Info = 3
    }

    public class DbItemResponse
    {

        //Array of Identifiers. One table may have 2 primary keys, in this case the PrimaryKeys dictionary should contain 2 items. May also contain all Items if PrimaryKeys are not available.
        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, object> PrimaryKeys { get; set; }
        public string OutputMessage { get; set; }
        //severity
        public DbOperationResponseSeverity Severity { get; set; }

        public DbItemResponse(DbOperationResponseSeverity severity, string outputMessage, Dictionary<string, object> primaryKeys)
        {
            Severity = severity;
            OutputMessage = outputMessage;
            PrimaryKeys = primaryKeys;
        }

        public string ItemIdentifiers
        {
            get
            {
                return string.Join(", ", PrimaryKeys.Select(pk => $"{pk.Key} : {pk.Value}"));

            }
        }

    }

    public class GeneralError
    {
        public string OutputMessage { get; set; }

        public DbOperationResponseSeverity Severity { get; set; }
        public GeneralError(DbOperationResponseSeverity severity, string outputMessage)
        {
            Severity = severity;
            OutputMessage = outputMessage;

        }
    }


}
