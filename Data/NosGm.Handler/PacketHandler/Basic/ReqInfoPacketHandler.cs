using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Packets.Packets.ClientPackets;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class ReqInfoPacketHandler : IPacketHandler
    {
        #region Instantiation

        public ReqInfoPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ReqInfo(ReqInfoPacket reqInfoPacket)
        {
            if (Session.Character == null)
            {
                return;
            }

            if (reqInfoPacket.Type == 6)
            {
                HandleModernEntityInfo(reqInfoPacket);
                return;
            }

            if (reqInfoPacket.Type == 5)
            {
                var npc = ServerManager.GetNpcMonster((short)reqInfoPacket.TargetTypeOrVNum);

                if (Session.CurrentMapInstance?.GetMonsterById(Session.Character.LastNpcMonsterId) is MapMonster monster &&
                    monster.Monster?.OriginalNpcMonsterVNum == reqInfoPacket.TargetTypeOrVNum)
                {
                    npc = ServerManager.GetNpcMonster(monster.Monster.NpcMonsterVNum);
                }

                if (npc != null)
                {
                    Session.SendPacket(npc.GenerateEInfo());
                }

                return;
            }

            if (reqInfoPacket.Type == 12)
            {
                if (Session.Character.Inventory != null)
                {
                    Session.SendPacket(Session.Character.Inventory
                        .LoadBySlotAndType((short)reqInfoPacket.TargetTypeOrVNum, InventoryType.Equipment)
                        ?.GenerateReqInfo());
                }

                return;
            }

            if (ServerManager.Instance.GetSessionByCharacterId(reqInfoPacket.TargetTypeOrVNum)?.Character is Character character)
            {
                Session.SendPacket(character.GenerateReqInfo());
            }
        }

        private void HandleModernEntityInfo(ReqInfoPacket reqInfoPacket)
        {
            if (!reqInfoPacket.TargetId.HasValue || Session.CurrentMapInstance == null)
            {
                return;
            }

            var targetId = reqInfoPacket.TargetId.Value;

            switch (reqInfoPacket.TargetTypeOrVNum)
            {
                case 1:
                    if (ServerManager.Instance.GetSessionByCharacterId(targetId)?.Character is Character character)
                    {
                        Session.SendPacket(character.GenerateReqInfo());
                    }

                    return;

                case 2:
                    var mapNpc = Session.CurrentMapInstance.Npcs.FirstOrDefault(npc => npc.MapNpcId == targetId);
                    if (mapNpc != null)
                    {
                        var npcInfo = ServerManager.GetNpcMonster(mapNpc.NpcVNum);
                        if (npcInfo != null)
                        {
                            Session.Character.LastNpcMonsterId = mapNpc.MapNpcId;
                            Session.SendPacket(npcInfo.GenerateEInfo(useClientLocalizedName: true));
                            return;
                        }
                    }

                    var mate = Session.CurrentMapInstance.Sessions.FirstOrDefault(session =>
                            session?.Character?.Mates != null && session.Character.Mates.Any(candidate =>
                                candidate.MateTransportId == targetId))?
                        .Character.Mates.FirstOrDefault(candidate => candidate.MateTransportId == targetId);

                    if (mate != null)
                    {
                        Session.SendPacket(mate.GenerateEInfo());
                    }

                    return;

                case 3:
                    var monster = Session.CurrentMapInstance.GetMonsterById(targetId);
                    if (monster == null)
                    {
                        return;
                    }

                    var monsterInfo = monster.Monster ?? ServerManager.GetNpcMonster(monster.MonsterVNum);
                    if (monsterInfo == null)
                    {
                        return;
                    }

                    Session.Character.LastNpcMonsterId = monster.MapMonsterId;
                    Session.SendPacket(monsterInfo.GenerateEInfo(useClientLocalizedName: true));
                    return;
            }
        }

        #endregion
    }
}