namespace Frostvein.Domain
{
    public enum MessageType : byte
    {
        Whisper = 0,
        PrivateChat = 1,
        Family = 2,
        Shout = 3,
        FamilyChat = 4,
        WhisperSupport = 5,
        WhisperGM = 6,
        Other = 7,
        Broadcast = 8,
        UpdateExploit = 9
    }

    public enum ChatMessageType : byte
    {
        Bubble = 1,
        Bubble2 = 2,
        Group = 3,
        TimeSpace = 4,
        Whisper = 5,
        Family = 6,
        LightYellow = 7,
        Whisper2 = 8,
        Whisper3 = 9,
        Yellow = 10,
        Red = 11,
        Green = 12,
        Grey = 13
    }
}