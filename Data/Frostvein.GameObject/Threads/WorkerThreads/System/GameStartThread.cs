using Frostvein.Configuration;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;


namespace Frostvein.GameObject.Frostvein.Thread.System
{
    public static class GameStartThread
    {
        public static void BattlePass(ClientSession Session)
        {
            if (GameConfiguration.BattlePassEnabled && ServerManager.Instance.ChannelId != 51)
            {
                Session.Character.LoadBattlePass();
            }

            if (GameConfiguration.BattlePassEnabled && ServerManager.Instance.ChannelId != 51)
            {
                Session.Character.DailyBattlePassRefresh();
                Session.Character.BattlePassQuestReset();
            }

            if (GameConfiguration.BattlePassEnabled && ServerManager.Instance.ChannelId != 51)
            {
                Session.SendPacket(Session.Character.GenerateBpQuest());
                Session.SendPacket(Session.Character.GenerateBp2Quest());
                Session.SendPacket(Session.Character.GenerateBptPacket());
                Session.SendPacket(Session.Character.GenerateBppPacket());
            }

            if (Session.Character.UnlockedBattlePassMultiplicator)
            {
                MessageExtension.SendGrey(Session, "Your Battle Pass Multiplicator has been loaded");
            }
        }

        public static void EditorMode(ClientSession Session)
        {
            if (Session.Character.EditorMode)
            {
                MessageExtension.SendYellow(Session, "You are in the Editor Mode.");
            }
        }

        public static void Configuration(ClientSession Session)
        {
            if (GameConfiguration.SceneOnCreate)
            {
                Session.SendPacket("scene 40");
            }

            if (ServerConfiguration.MaintenanceMode)
            {
                if (Session.Character.Authority == AuthorityType.User)
                {
                    Session.Disconnect();
                }
            }

            if (EventConfiguration.IsActivated)
            {
                Session.SendPacket($"evtb " +
                    $"{EventConfiguration.UpgradeEquipment} " +
                    $"{EventConfiguration.RarifyEquipment} " +
                    $"{EventConfiguration.UpgradeSpecialist} " +
                    $"{EventConfiguration.PerfectSpecialist} " +
                    $"{EventConfiguration.FamilyEXP} " +
                    $"{EventConfiguration.SealedVessel} " +
                    $"{EventConfiguration.EXP} " +
                    $"{EventConfiguration.Gold} " +
                    $"{EventConfiguration.Reputation} " +
                    $"{EventConfiguration.Drop} " +
                    $"{EventConfiguration.UpgradeRune} " +
                    $"{EventConfiguration.UpgradeTattoo} " +
                    $"{EventConfiguration.FishEXP} " +
                    $"{EventConfiguration.FishCook} " +
                    $"{EventConfiguration.DoubleBox} " +
                    $"{EventConfiguration.Satity} " +
                    $"{EventConfiguration.PartnerSP} " +
                    $"{EventConfiguration.PartnerEXP} ");
            }
        }

        public static void GetIp(ClientSession Session)
        {
            Session.Character.CurrentIp = Session.IpAddress.Substring(6, Session.IpAddress.LastIndexOf(':') - 6);
        }

        public static void GenerateBn(ClientSession Session)
        {
            Session.SendPacket($"bn 0 {GameConfiguration.BN0}");
            Session.SendPacket($"bn 1 {GameConfiguration.BN1}");
            Session.SendPacket($"bn 2 {GameConfiguration.BN2}");
            Session.SendPacket($"bn 3 {GameConfiguration.BN3}");
            Session.SendPacket($"bn 4 {GameConfiguration.BN4}");
            Session.SendPacket($"bn 5 {GameConfiguration.BN5}");
            Session.SendPacket($"bn 6 {GameConfiguration.BN6}");
            Session.SendPacket($"bn 7 {GameConfiguration.BN7}");
            Session.SendPacket($"bn 8 {GameConfiguration.BN8}");
            Session.SendPacket($"bn 9 {GameConfiguration.BN9}");
        }
       
        public static void LoadSeason(ClientSession Session, SeasonType season)
        {
            string currentSeason = GameConfiguration.Season.ToString();
            switch (season)
            {
                case SeasonType.Elements:
                    currentSeason = "Elements";
                    MessageExtension.SendHero(Session, $"Current Raid Season: {currentSeason}");
                    break;

                case SeasonType.Enlightment:
                    currentSeason = "Enlightment";
                    MessageExtension.SendHero(Session, $"Current Raid Season: {currentSeason}");
                    break;

                case SeasonType.Hope:
                    currentSeason = "Hope";
                    MessageExtension.SendHero(Session, $"Current Raid Season: {currentSeason}");
                    break;

                case SeasonType.Despair:
                    currentSeason = "Despair";
                    MessageExtension.SendHero(Session, $"Current Raid Season: {currentSeason}");
                    break;
            }
        }
    }
}
