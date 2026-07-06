namespace NosTale.ServiceManager.LogService.Configuration
{
    public static class LogConfiguration
    {
        public static readonly string ConnectionString = "mongodb://localhost:27017";
        public static readonly string DatabaseName = "LogService";

        public static readonly string UpgradeEquipmentLogTable = "UpgradeEquipmentLog";
        public static readonly string UpgradeSpecialistLogTable = "UpgradeSpecialistLog";
        public static readonly string PerfectSpecialistLogTable = "PerfectSpecialistLog";
        public static readonly string SumResistanceLogTable = "SumResistanceLog";
        public static readonly string RarifyEquipmentLogTable = "RarifyEquipmentLog";
        public static readonly string ShopBuyLogTable = "ShopBuyLog";
        public static readonly string ShopSellLogTable = "ShopSellLog";
        public static readonly string CommandUseLogTable = "CommandUseLog";
        public static readonly string BazaarModLogTable = "BazaarModLog";
        public static readonly string BazaarBuyLogTable = "BazaarBuyLog";
        public static readonly string BazaarSellLogTable = "BazaarSellLog";
        public static readonly string BazaarRemoveLogTable = "BazaarRemoveLog";
        public static readonly string ChatLogTable = "ChatLog";
        public static readonly string FamilyCreateLogTable = "FamilyCreateLog";
        public static readonly string RaidFinishLogTable = "RaidFinishLog";
        public static readonly string RaidboxOpenLogTable = "RaidboxOpenLog";
        public static readonly string MysteryBoxOpenLogTable = "MysteryBoxOpenLog";
        public static readonly string ArenaEventLogTable = "ArenaEventLog";
        public static readonly string WatchdogLogTable = "WatchdogLog";
        public static readonly string KickLogTable = "KickLog";
        public static readonly string BanLogTable = "BanLog";
        public static readonly string ExploitLogTable = "ExploitLog";
        public static readonly string ServerErrorLogTable = "ServerErrorLog";
        public static readonly string ServerInfoLogTable = "ServerInfoLog";
        public static readonly string EventLogTable = "EventLog";
        public static readonly string UpgradeFairyLogTable = "UpgradeFairy";
    }
}
