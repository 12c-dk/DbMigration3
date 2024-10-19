using System.Text.Json;
using DbMigration.Common.Legacy.ClientStorage.Handlers;
using DbMigration.Common.Legacy.Handlers;
using Microsoft.Extensions.Logging;

namespace DbMigration.Common.Legacy.Messaging
{
    public class MessageFilterHandler
    {
        private readonly CopyJobHandler _copyJobHandler;
        private readonly LoadSourceRowsHandler _loadSourceRowsHandler;
        private readonly TransformHandler _transformHandler;
        private readonly PushHandler _pushHandler;
        private readonly ILogger<MessageFilterHandler> _log;

        public MessageFilterHandler(CopyJobHandler copyJobHandler, LoadSourceRowsHandler loadSourceRowsHandler, TransformHandler transformHandler, ILogger<MessageFilterHandler> log, PushHandler pushHandler)
        {
            _copyJobHandler = copyJobHandler;
            _loadSourceRowsHandler = loadSourceRowsHandler;
            _transformHandler = transformHandler;
            _log = log;
            _pushHandler = pushHandler;
        }

        public async Task HandleMessage(string messageJson)
        {
            JsonDocument parsed = JsonDocument.Parse(messageJson);

            if (!parsed.RootElement.TryGetProperty("MessageType", out JsonElement myProperty))
            {
                throw new ApplicationException("Received message without required MessageType property.");
            }

            string messageType = myProperty.GetString();

            switch (messageType)
            {
                case nameof(MessageType.CopyJob):
                    var message = JsonSerializer.Deserialize<MessageBase<CopyJobMessage>>(messageJson);
                    await _copyJobHandler.Handle(message);
                    break;
                case nameof(MessageType.LoadSourceRows):
                    var loadSourceRowsMessage = JsonSerializer.Deserialize<MessageBase<LoadSourceRowsMessage>>(messageJson);
                    await _loadSourceRowsHandler.Handle(loadSourceRowsMessage);
                    break;
                case nameof(MessageType.Transform):
                    var transformMessage = JsonSerializer.Deserialize<MessageBase<TransformMessage>>(messageJson);
                    await _transformHandler.Handle(transformMessage);
                    break;
                case nameof(MessageType.PushRows):
                    var pushMessage = JsonSerializer.Deserialize<MessageBase<PushMessage>>(messageJson);
                    await _pushHandler.Handle(pushMessage);
                    break;
                default:
                    _log.LogError($"MessageFilterHandler Received unknown message type: {messageType}");
                    break;
            }

        }
    }
}
