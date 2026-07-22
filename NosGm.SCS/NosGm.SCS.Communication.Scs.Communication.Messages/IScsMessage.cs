namespace NosGm.SCS.Communication.Scs.Communication.Messages
{
    public interface IScsMessage
    {
        string MessageId { get; }

        string RepliedMessageId { get; set; }
    }
}
