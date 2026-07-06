using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Miniland
{
    public class MLEditPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MLEditPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MinilandEdit(MLEditPacket mlEditPacket)
        {
            if (mlEditPacket != null && mlEditPacket.Parameters != null)
                switch (mlEditPacket.Type)
                {
                    case 1:
                        Session.Character.MinilandMessage = mlEditPacket.Parameters.Truncate(50);
                        Session.SendPacket($"mlintro {Session.Character.MinilandMessage.Replace(' ', '^')}");
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(
                                Language.Instance.GetMessageFromKey("MINILAND_INFO_CHANGED")));
                        break;

                    case 2:
                        MinilandState state;
                        Enum.TryParse(mlEditPacket.Parameters, out state);

                        switch (state)
                        {
                            case MinilandState.Private:
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("MINILAND_PRIVATE"),
                                        0));

                                //Need to be review to permit one friend limit on the miniland
                                Session.Character.Miniland.Sessions.Where(s => s.Character != Session.Character)
                                    .ToList()
                                    .ForEach(s => ServerManager.Instance.ChangeMap(s.Character.CharacterId,
                                        s.Character.MapId, s.Character.MapX, s.Character.MapY));
                                break;

                            case MinilandState.Lock:
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("MINILAND_LOCK"),
                                        0));
                                Session.Character.Miniland.Sessions.Where(s => s.Character != Session.Character)
                                    .ToList()
                                    .ForEach(s => ServerManager.Instance.ChangeMap(s.Character.CharacterId,
                                        s.Character.MapId, s.Character.MapX, s.Character.MapY));
                                break;

                            case MinilandState.Open:
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("MINILAND_PUBLIC"),
                                        0));
                                break;
                        }

                        Session.Character.MinilandState = state;
                        break;
                }
        }

        #endregion
    }
}