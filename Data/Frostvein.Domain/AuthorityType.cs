namespace Frostvein.Domain
{
    public enum AuthorityType : short
    {
        Closed = -3,
        Banned = -2,
        Unconfirmed = -1,
        User = 0,
        GS = 1,
        GM = 2,
        ADMIN = 3,
        DEV = 4
    }
}