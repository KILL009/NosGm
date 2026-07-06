using Frostvein.Configuration;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Packets.ClientPackets;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class BpPselPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BpPselPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task BattlePassPrizeSelect(BpPselPacket packet)
        {
            if (!GameConfiguration.BattlePassEnabled || ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            bool isPremium = Session.Character.HasPremiumBattlePass;
            bool havePoint = Session.Character.BattlePassPoints >= (packet.Level == null ? 1 : packet.Level < 0 ? 1 : packet.Level + 1) * 50;

            if (packet.Position == 1 && !isPremium)
            {
                return;
            }

            if (packet.Level != null && !havePoint)
            {
                return;
            }


            if (packet.Position < 2 && packet.Level != null)
            {
                byte level = (byte)(packet.Level + 1);
                var prize = ServerManager.Instance.BattlePassPrizes.FirstOrDefault(b => b.Level == level);

                if (packet.Position == 0)
                {
                    if (Session.Character.BattlePassAccountLogs.FirstOrDefault(b => b.Level == prize.Level && b.IsPremium == false) == null)
                    {
                        Session.Character.GiftAdd(prize.ItemVNum, prize.Amount);
                        Session.Character.BattlePassAccountLogs.Add(new BattlePassAccountLogDTO { AccountId = Session.Character.AccountId, Level = prize.Level, IsPremium = false });
                    }
                }
                else
                {
                    if (Session.Character.BattlePassAccountLogs.FirstOrDefault(b => b.Level == prize.Level && b.IsPremium == true) == null)
                    {
                        Session.Character.GiftAdd(prize.ItemVNumPremium, prize.AmountPremium);
                        Session.Character.BattlePassAccountLogs.Add(new BattlePassAccountLogDTO { AccountId = Session.Character.AccountId, Level = prize.Level, IsPremium = true });
                    }
                }
            }
            else
            {
                int level = (int)Math.Floor((decimal)(Session.Character.BattlePassPoints / 50));
                foreach (var prize in ServerManager.Instance.BattlePassPrizes.Where(b => b.Level <= level))
                {
                    if (Session.Character.BattlePassAccountLogs.FirstOrDefault(b => b.Level == prize.Level) == null)
                    {
                        if (isPremium)
                        {
                            Session.Character.SendItem(Session.Character.CharacterId, prize.ItemVNumPremium, prize.AmountPremium, 0, 0, 0, false);
                            Session.Character.SendItem(Session.Character.CharacterId, prize.ItemVNum, prize.Amount, 0, 0, 0, false);
                            Session.Character.BattlePassAccountLogs.Add(new BattlePassAccountLogDTO { AccountId = Session.Character.AccountId, Level = prize.Level, IsPremium = true });
                            Session.Character.BattlePassAccountLogs.Add(new BattlePassAccountLogDTO { AccountId = Session.Character.AccountId, Level = prize.Level, IsPremium = false });
                        }
                        else
                        {
                            Session.Character.SendItem(Session.Character.CharacterId, prize.ItemVNum, prize.Amount, 0, 0, 0, false);
                            Session.Character.BattlePassAccountLogs.Add(new BattlePassAccountLogDTO { AccountId = Session.Character.AccountId, Level = prize.Level, IsPremium = false });
                        }
                    }
                }
            }

            Session.SendPacket(Session.Character.GenerateBppPacket());

        }

        #endregion
    }
}

