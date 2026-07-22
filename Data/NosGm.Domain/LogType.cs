namespace NosGm.Domain
{
    public enum LogType : byte
    {
        ERROR,
        WARNING,
        INFO,
        LOAD,
        Character,
        CharacterAction,
        CharacterCommand,
        CharacterStaffCommand,
        Trade,
        Bet,
        UpgradeEquipment,
        UpgradeSpecialistCard,
        UpgradeSpecialistCardPerfection,
        SumResistance,
        BazaarBuy,
        BazaarSell,
        BazaarMod,
        Ban,
        Kick,
        Mute,
        Exploit,
    }
}