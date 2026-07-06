using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class SummonHandler : IPacketHandler
    {
        #region Instantiation

        public SummonHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Summon(SummonPacket summonPacket)
        {
            var random = new Random();
            if (summonPacket != null)
            {
                if (summonPacket.CharacterName == "*")
                {
                    foreach (var session in ServerManager.Instance.Sessions.Where(s =>
                        s.Character != null && s.Character.CharacterId != Session.Character.CharacterId))
                    {
                        // clear any shop or trade on target character
                        Session.Character.DisposeShopAndExchange();
                        if (!session.Character.IsChangingMapInstance && Session.HasCurrentMapInstance)
                        {
                            var possibilities = new List<MapCell>();
                            for (short x = -6, y = -6; x < 6 && y < 6; x++, y++)
                                possibilities.Add(new MapCell { X = x, Y = y });

                            var mapXPossibility = Session.Character.PositionX;
                            var mapYPossibility = Session.Character.PositionY;
                            foreach (var possibility in possibilities.OrderBy(s => random.Next()))
                            {
                                mapXPossibility = (short)(Session.Character.PositionX + possibility.X);
                                mapYPossibility = (short)(Session.Character.PositionY + possibility.Y);
                                if (!Session.CurrentMapInstance.Map.IsBlockedZone(mapXPossibility, mapYPossibility))
                                    break;
                            }

                            if (Session.Character.Miniland == Session.Character.MapInstance)
                                ServerManager.Instance.JoinMiniland(session, Session);
                            else
                                ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                    Session.Character.MapInstanceId, mapXPossibility, mapYPossibility);
                        }
                    }
                }
                else
                {
                    var targetSession =
                        ServerManager.Instance.GetSessionByCharacterName(summonPacket.CharacterName);
                    if (targetSession?.Character.IsChangingMapInstance == false)
                    {
                        Session.Character.DisposeShopAndExchange();
                        ServerManager.Instance.ChangeMapInstance(targetSession.Character.CharacterId,
                            Session.Character.MapInstanceId, (short)(Session.Character.PositionX + 1),
                            (short)(Session.Character.PositionY + 1));
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"),
                                0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SummonPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}