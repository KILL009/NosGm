namespace Frostvein.Domain
{
    public enum MapInstanceType
    {
        BaseMapInstance,
        NormalInstance,
        LodInstance,
        TimeSpaceInstance,
        RaidInstance,
        FamilyRaidInstance,
        GlacernonShip,
        Act4ShipAngel,
        Act4ShipDemon,
        Act4Morcos,
        Act4Hatus,
        Act4Calvina,
        Act4Berios,
        EventGameInstance,
        CaligorInstance,
        IceBreakerInstance,
        ArenaInstance,
        GemmeStoneInstance,
        TalentArenaMapInstance,
        Act4Instance,
        PVPInstance,
        RainbowBattleInstance,
        SheepGameInstance,
        MapPvp = ArenaInstance | TalentArenaMapInstance | Act4Instance,
        Act7Ship = 32,
        SealedVesselsMap = 33,
        CustomInstance,
        CelestialSpire,
    }
}