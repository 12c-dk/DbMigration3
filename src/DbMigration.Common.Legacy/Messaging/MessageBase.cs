using System.Text.Json.Serialization;

namespace DbMigration.Common.Legacy.Messaging
{
    public class MessageBase<T> where T : IMessageData
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageType MessageType { get; set; }
        public Guid MessageId { get; set; }
        public string CorrelationId { get; set; }
        /// <summary>
        /// The name of the service/messageHandler that issued the message
        /// </summary>
        public string IssuedBy { get; set; }
        public bool DeleteAfterProcessing { get; set; } = true;
        public string MessageFilePath { get; set; }

        public T Data { get; set; }

        //JobId?
        //JobDefinitionId?

        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public void Validate(bool expectMessageFilePath = false)
        {
            if (MessageId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(MessageId));
            }

            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }

            if (expectMessageFilePath && string.IsNullOrEmpty(MessageFilePath))
            {
                throw new ArgumentException(nameof(MessageFilePath));
            }

            Data.IsValid();

        }


    }
}
