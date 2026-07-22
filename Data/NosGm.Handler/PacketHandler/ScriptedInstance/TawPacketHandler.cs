using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.ScriptedInstance
{
    public class TawPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TawPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SearchName(TawPacket packet)
        {
            var at = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s =>
                s.Any(o => o.Session?.Character?.Name == packet.Username && Session.CurrentMapInstance != null));
            if (at != null)
            {
                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                    at.FirstOrDefault(s => s.Session != null).Session.CurrentMapInstance.MapInstanceId, 69, 100);

                var zenas = at.OrderBy(s => s.Order).FirstOrDefault(s =>
                    s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ZENAS);
                var erenia = at.OrderBy(s => s.Order).FirstOrDefault(s =>
                    s.Session != null && !s.Dead && s.ArenaTeamType == ArenaTeamType.ERENIA);
                Session.SendPacket(Session.Character.GenerateTaM(0));
                Session.SendPacket(Session.Character.GenerateTaM(3));
                Session.SendPacket("taw_sv 0");
                Session.SendPacket(zenas?.Session.Character.GenerateTaP(0, true));
                Session.SendPacket(erenia?.Session.Character.GenerateTaP(2, true));
                Session.SendPacket(zenas?.Session.Character.GenerateTaFc(0));
                Session.SendPacket(erenia?.Session.Character.GenerateTaFc(1));
            }
            else
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("USER_NOT_FOUND_IN_ARENA")));
            }
        }

        #endregion
    }
}