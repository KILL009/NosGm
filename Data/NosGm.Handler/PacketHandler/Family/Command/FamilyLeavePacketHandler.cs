using NosGm.Packets.Packets.FamilyCommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Reactive.Linq;

namespace NosGm.Handler.PacketHandler.Family.Command
{
    public class FamilyLeavePacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyLeavePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyLeave(FamilyLeavePacket packet)
        {
            if (Session.Character.Family == null || Session.Character.FamilyCharacter == null)
            {
                return;
            }
            if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                return;
            }

            if (Session.CurrentMapInstance.MapInstanceType.Equals(MapInstanceType.LodInstance))
            {
                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY, true);
            }

            if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANNOT_LEAVE_FAMILY")));
                return;
            }

            var familyId = Session.Character.Family.FamilyId;

            DAOFactory.FamilyCharacterDAO.Delete(Session.Character.CharacterId);

            Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                $"[FamilyLeave][{Session.Character.Family.FamilyId}]");
            Logger.LogUserEvent("GUILDLEAVE", Session.GenerateIdentity(),
                $"[FamilyLeave][{Session.Character.Family.FamilyId}]");

            Session.Character.Family.InsertFamilyLog(FamilyLogType.FamilyManaged, Session.Character.Name);
            Session.Character.LastFamilyLeave = DateTime.Now.Ticks;
            Session.Character.Family = null;

            ServerManager.Instance.FamilyRefresh(familyId);

            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
            {
                if (Session == null)
                {
                    return;
                }

                ServerManager.Instance.FamilyRefresh(familyId);
            });
            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(o =>
            {
                if (Session == null)
                {
                    return;
                }

                ServerManager.Instance.FamilyRefresh(familyId);
            });

            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY, true);
            Session.SendPacket(StaticPacketHelper.Cancel(2));
            Session.Character.LastFamilyAction = DateTime.Now;
        }

        #endregion
    }
}