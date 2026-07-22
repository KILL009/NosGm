using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;

namespace NosGm.Handler.PacketHandler.Command
{
    public class MobRainHandler : IPacketHandler
    {
        #region Instantiation

        public MobRainHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MobRain(MobRainPacket mobRainPacket)
        {
            if (mobRainPacket != null)
            {
                //Session.AddLogsCmd(mobRainPacket);
                if (Session.IsOnMap && Session.HasCurrentMapInstance)
                {
                    var npcmonster = ServerManager.GetNpcMonster(mobRainPacket.NpcMonsterVNum);
                    if (npcmonster == null) return;

                    var SummonParameters = new List<MonsterToSummon>();
                    SummonParameters.AddRange(Session.Character.MapInstance.Map.GenerateMonsters(
                        mobRainPacket.NpcMonsterVNum, mobRainPacket.Amount, mobRainPacket.IsMoving,
                        new List<EventContainer>()));
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(1),
                        new EventContainer(Session.CurrentMapInstance, EventActionType.SPAWNMONSTERS,
                            SummonParameters));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MobRainPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}