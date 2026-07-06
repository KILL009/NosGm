using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Mate
{
    public class PslPacketHandler : IPacketHandler
    {
        #region Instantiation

        public PslPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Psl(PslPacket pslPacket)
        {
            var mate = Session?.Character?.Mates?.ToList().Find(s => s.IsTeamMember && s.MateType == MateType.Partner);

            if (mate == null) return;

            if (mate.Sp == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_PSP"), 0));
                return;
            }

            if (!mate.IsUsingSp && !mate.CanUseSp())
            {
                var spRemainingCooldown = mate.GetSpRemainingCooldown();
                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("STAY_TIME"), spRemainingCooldown), 11));
                Session.SendPacket($"psd {spRemainingCooldown}");
                return;
            }

            if (pslPacket.Type == 0)
            {
                if (mate.IsUsingSp)
                {
                    mate.RemoveSp();
                    mate.StartSpCooldown();
                }
                else
                {
                    Session.SendPacket("pdelay 5000 3 #psl^1");
                    Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(2, 2, mate.MateTransportId), mate.PositionX, mate.PositionY);
                }
            }
            else
            {
                mate.IsUsingSp = true;
                MateHelper.Instance.AddPartnerBuffs(Session, mate);
                Session.SendPacket(mate.GenerateCond());
                Session.Character.MapInstance.Broadcast(mate.GenerateCMode(mate.Sp.Instance.Item.Morph));
                Session.SendPacket(mate.Sp.GeneratePski());
                Session.SendPacket(mate.GenerateScPacket(true));
                Session.Character.MapInstance.Broadcast(mate.GenerateOut());

                var isAct4 = ServerManager.Instance.ChannelId == 51;

                foreach (var s in Session.CurrentMapInstance.Sessions.Where(s => s.Character != null))
                {
                    if (!isAct4 || Session.Character.Faction == s.Character.Faction)
                    {
                        s.SendPacket(mate.GenerateIn(false, isAct4));
                    }
                    else
                    {
                        s.SendPacket(mate.GenerateIn(true, isAct4, s.Account.Authority));
                    }
                }

                Session.SendPacket(Session.Character.GeneratePinit());
                Session.Character.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 196));
            }
        }

        #endregion
    }
}