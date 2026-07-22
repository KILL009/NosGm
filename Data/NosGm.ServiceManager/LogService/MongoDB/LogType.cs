namespace NosTale.ServiceManager.LogService
{
    public enum LogType : byte
    {
        UpgradeEquipment,
        UpgradeSpecialist,
        UpgradePerfection,

        ShopBuy,
        ShopSell,

        Event,
        Exploit,

        ServerInfo,
        ServerError,

        UpgradeFairy,
    }
}
