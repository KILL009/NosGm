using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class MonsterInfoHandler : IPacketHandler
    {
        #region Instantiation

        public MonsterInfoHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MonsterInfo(MonsterInfoPacket NpcInfoPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[MonsterInfo]");
            MapMonster monster = Session.CurrentMapInstance.GetMonsterById(Session.Character.LastNpcMonsterId);
            if (monster != null)
            {
                int distance = Map.GetDistance(new MapCell
                {
                    X = Session.Character.PositionX,
                    Y = Session.Character.PositionY
                }, new MapCell
                {
                    X = monster.MapX,
                    Y = monster.MapY
                });
                if (monster.IsAlive)
                {
                    // Check key that show bad values.
                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("NPC_INFORMATIONS"), monster.MapMonsterId,
                            monster.Monster.Name, monster.MonsterVNum, monster.MapId, monster.MapX, monster.MapY), 12));
                }
            }
        }

        #endregion
    }
}
