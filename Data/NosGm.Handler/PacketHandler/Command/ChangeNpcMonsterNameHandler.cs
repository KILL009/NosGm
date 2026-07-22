using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ChangeNpcMonsterNameHandler : IPacketHandler
    {
        #region Instantiation

        public ChangeNpcMonsterNameHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void CustomNpcMonsterName(ChangeNpcMonsterNamePacket changeNpcMonsterNamePacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                if (Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId) is MapNpc npc)
                {
                    if (DAOFactory.MapNpcDAO.LoadById(npc.MapNpcId) is MapNpcDTO npcDTO)
                    {
                        npc.Name = changeNpcMonsterNamePacket.Name;
                        npcDTO.Name = changeNpcMonsterNamePacket.Name;
                        DAOFactory.MapNpcDAO.Update(ref npcDTO);

                        Session.CurrentMapInstance.Broadcast(npc.GenerateIn());
                    }
                }
                else if (Session.CurrentMapInstance.GetMonsterById(Session.Character.LastNpcMonsterId) is MapMonster
                    monster)
                {
                    if (DAOFactory.MapMonsterDAO.LoadById(monster.MapMonsterId) is MapMonsterDTO monsterDTO)
                    {
                        monster.Name = changeNpcMonsterNamePacket.Name;
                        monsterDTO.Name = changeNpcMonsterNamePacket.Name;
                        DAOFactory.MapMonsterDAO.Update(ref monsterDTO);

                        Session.CurrentMapInstance.Broadcast(monster.GenerateIn());
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NPCMONSTER_NOT_FOUND"), 11));
                }
            }
        }

        #endregion
    }
}