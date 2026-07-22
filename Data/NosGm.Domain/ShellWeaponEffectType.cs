namespace NosGm.Domain
{
    public enum ShellWeaponEffectType : byte
    {
        DamageImproved = 1,
        PercentageTotalDamage = 2,
        MinorBleeding = 3,
        Bleeding = 4,
        HeavyBleeding = 5,
        Blackout = 6,
        Freeze = 7,
        DeadlyBlackout = 8,
        DamageIncreasedtothePlant = 9, //Not implemented
        DamageIncreasedtotheAnimal = 10, //Not implemented
        DamageIncreasedtotheEnemy = 11, //Not implemented
        DamageIncreasedtotheUnDead = 12, //Not implemented
        DamageincreasedtotheSmallMonster = 13, //Not implemented
        DamageincreasedtotheBigMonster = 14, //Not implemented
        CriticalChance = 15, //Except Sticks
        CriticalDamage = 16, //Except Sticks
        AntiMagicDisorder = 17, //Only Sticks  //Not implemented
        IncreasedFireProperties = 18,
        IncreasedWaterProperties = 19,
        IncreasedLightProperties = 20,
        IncreasedDarkProperties = 21,
        IncreasedElementalProperties = 22,
        ReducedMPConsume = 23, //Not implemented
        HPRecoveryForKilling = 24, //Not implemented
        MPRecoveryForKilling = 25, //Not implemented
        SLDamage = 26,
        SLDefence = 27,
        SLElement = 28,
        SLHP = 29,
        SLGlobal = 30,
        GainMoreGold = 31,
        GainMoreXP = 32, //Not implemented
        GainMoreCXP = 33, //Not implemented
        PercentageDamageInPVP = 34,
        ReducesPercentageEnemyDefenceInPVP = 35,
        ReducesEnemyFireResistanceInPVP = 36,
        ReducesEnemyWaterResistanceInPVP = 37,
        ReducesEnemyLightResistanceInPVP = 38,
        ReducesEnemyDarkResistanceInPVP = 39,
        ReducesEnemyAllResistancesInPVP = 40,
        NeverMissInPVP = 41, //Not implemented
        PVPDamageAt15Percent = 42, //Not implemented
        ReducesEnemyMPInPVP = 43, //Not implemented
        InspireFireResistanceWithPercentage = 44, //Not implemented
        InspireWaterResistanceWithPercentage = 45, //Not implemented
        InspireLightResistanceWithPercentage = 46, //Not implemented
        InspireDarkResistanceWithPercentage = 47, //Not implemented
        GainSPForKilling = 48, //Not implemented
        IncreasedPrecision = 49, //Not implemented
        IncreasedFocus = 50 //Not implemented
    }
}