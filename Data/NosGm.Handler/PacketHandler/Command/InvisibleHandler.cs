using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class InvisibleHandler : IPacketHandler
    {
        #region Instantiation

        public InvisibleHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Invisible(InvisiblePacket invisiblePacket)
        {
            //Session.AddLogsCmd(invisiblePacket);
            Session.Character.Invisible = !Session.Character.Invisible;
            Session.Character.InvisibleGm = !Session.Character.InvisibleGm;
            Session.SendPacket(Session.Character.GenerateInvisible());
            Session.SendPacket(Session.Character.GenerateEq());
            Session.SendPacket(Session.Character.GenerateCMode());

            if (Session.Character.InvisibleGm)
            {
                Session.Character.Mates.Where(s => s.IsTeamMember).ToList()
                    .ForEach(s => Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                Session.CurrentMapInstance?.Broadcast(Session,
                    StaticPacketHelper.Out(UserType.Player, Session.Character.CharacterId), ReceiverType.AllExceptMe);
            }
            else
            {
                foreach (var teamMate in Session.Character.Mates.Where(m => m.IsTeamMember))
                {
                    teamMate.PositionX = Session.Character.PositionX;
                    teamMate.PositionY = Session.Character.PositionY;
                    teamMate.UpdateBushFire();
                    foreach (var s in Session.CurrentMapInstance.Sessions.Where(s => s.Character != null))
                        if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                            s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                        else
                            s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51,
                                s.Account.Authority));
                    Session.SendPacket(Session.Character.GeneratePinit());
                    Session.Character.Mates.ForEach(s => Session.SendPacket(s.GenerateScPacket()));
                    Session.SendPackets(Session.Character.GeneratePst());
                }

                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(),
                    ReceiverType.AllExceptMe);
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(),
                    ReceiverType.AllExceptMe);
            }
        }

        #endregion
    }
}