namespace DbMigration.Common.Legacy.Messaging
{
    /// <summary>
    /// This is a super class for MessageBase.Data models. MessageBase.Data needs to be of a type that inherits from this super class. 
    /// </summary>
    public interface IMessageData
    {
        public bool IsValid();

    }
}
