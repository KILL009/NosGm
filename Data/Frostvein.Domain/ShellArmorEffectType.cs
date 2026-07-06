namespace Frostvein.Domain
{
    public enum ShellArmorEffectType : byte
    {
        CloseDefence = 51,
        DistanceDefence = 52,
        MagicDefence = 53,
        PercentageTotalDefence = 54,
        ReducedMinorBleeding = 55,
        ReducedBleedingAndMinorBleeding = 56,
        ReducedAllBleedingType = 57,
        ReducedStun = 58,
        ReducedAllStun = 59,
        ReducedParalysis = 60, //Not implemented
        ReducedFreeze = 61,
        ReducedBlind = 62, //Not implemented
        ReducedSlow = 63, //Not implemented
        ReducedArmorDeBuff = 64, //Not implemented
        ReducedShock = 65, //Not implemented
        ReducedPoisonParalysis = 66, //Not implemented
        ReducedAllNegativeEffect = 67,
        RecoveryHPOnRest = 68,
        RecoveryHP = 69,
        RecoveryMPOnRest = 70,
        RecoveryMP = 71,
        RecoveryHPInDefence = 72, //Not implemented
        ReducedCritChanceRecive = 73,
        IncreasedFireResistence = 74,
        IncreasedWaterResistence = 75,
        IncreasedLightResistence = 76,
        IncreasedDarkResistence = 77,
        IncreasedAllResistence = 78,
        ReducedPrideLoss = 79, //Not implemented
        ReducedProductionPointConsumed = 80, //Not implemented
        IncreasedProductionPossibility = 81, //Not implemented
        IncreasedRecoveryItemSpeed = 82, //Not implemented
        PercentageAllPVPDefence = 83,
        CloseDefenceDodgeInPVP = 84,
        DistanceDefenceDodgeInPVP = 85,
        IgnoreMagicDamage = 86,
        DodgeAllAttacksInPVP = 87,
        ProtectMPInPVP = 88, //Not implemented
        FireDamageImmuneInPVP = 89, //Not implemented
        WaterDamageImmuneInPVP = 90, //Not implemented
        LightDamageImmuneInPVP = 91, //Not implemented
        DarkDamageImmuneInPVP = 92, //Not implemented
        AbsorbDamagePercentageA = 93, //Not implemented
        AbsorbDamagePercentageB = 94, //Not implemented
        AbsorbDamagePercentageC = 95, //Not implemented
        IncreaseEvasiveness = 96 //Not implemented
    }
}