namespace Frostvein.Configuration.GameEvent
{
    public static class InstantBattleConfiguration
    {
        public static bool DisablePlayerLimit = true;
        public static short GoldEarned(short Level) { return (short)(1000 * Level); }
        public static short ReputationEarned(short Level) { return (short)(1000 * Level); }
        public static short FamilyXPEarned(short Level) { return (short)(1000 * Level); }

    }
}
