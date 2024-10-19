namespace DbMigration.Common.Legacy.Messaging;

public enum MessageType
{
    None = 0,
    CopyJob = 1,
    LoadSourceRows = 2,
    Transform = 3,
    PushRows = 4
}