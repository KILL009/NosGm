namespace Frostvein.Domain
{
    public enum ReceiverType : byte
    {
        Unknown = 0,
        All = 1,
        AllExceptMe = 2,
        AllInRange = 3,
        OnlySomeone = 4,
        AllNoEmoBlocked = 5,
        AllNoHeroBlocked = 6,
        Group = 7,
        AllExceptGroup = 8,
        AllExceptMeAct4 = 9,
        OnlySomeLevel = 10,
        AllInTimeSpace = 11,
    }
}