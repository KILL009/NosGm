using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class ComplimentPacketHandler : IPacketHandler
    {
        #region Instantiation

        public ComplimentPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ComplimentAsync(ComplimentPacket complimentPacket)
        {
            if (complimentPacket != null)
            {
                //Sess = Target
                //Session = Yourself
                if (Session.Character.CharacterId == complimentPacket.CharacterId)
                {
                    return;
                }

                ClientSession Sess = ServerManager.Instance.GetSessionByCharacterId(complimentPacket.CharacterId);

                if (Sess != null)
                {
                    if (Session.Character.LastDuelInvite.AddSeconds(5) > DateTime.Now)
                    {
                        Session.SendPacket("info You have to wait 5 seconds before inviting someone!");
                        return;
                    }

                    if (Session.Character.DuelCount == 5)
                    {
                        Session.SendPacket("info You already participated in duels 5 times today");
                        return;
                    }

                    if (Sess.Character.DuelCount == 5)
                    {
                        Session.SendPacket($"info {Sess.Character.Name} already participated in duels 5 times today");
                        return;
                    }

                    Sess.SendPacket($"dlg #guri^2001^0^{Session.Character.CharacterId} #guri^2001^1^{Session.Character.CharacterId} {Session.Character.Name} invited you to a Duel.\nDo you want to accept?");
                    Session.Character.LastDuelInvite = DateTime.Now;
                }
            }
        }

        #endregion
    }
}