using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class AddNpcHandler : IPacketHandler
    {
        #region Instantiation

        public AddNpcHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task AddNpc(AddNpcPacket addNpcPacket)
        {
            if (addNpcPacket != null)
            {

                if (!Session.HasCurrentMapInstance) return;

                var npcmonster = ServerManager.GetNpcMonster(addNpcPacket.NpcVNum);
                if (npcmonster == null) return;

                var newNpc = new MapNpcDTO
                {
                    NpcVNum = addNpcPacket.NpcVNum,
                    MapY = Session.Character.PositionY,
                    MapX = Session.Character.PositionX,
                    MapId = Session.Character.MapInstance.Map.MapId,
                    Position = Session.Character.Direction,
                    IsMoving = addNpcPacket.IsMoving,
                    MapNpcId = ServerManager.Instance.GetNextNpcId()
                };
                if (!DAOFactory.MapNpcDAO.DoesNpcExist(newNpc.MapNpcId))
                {
                    DAOFactory.MapNpcDAO.Insert(newNpc);
                    if (DAOFactory.MapNpcDAO.LoadById(newNpc.MapNpcId) is MapNpcDTO npcDTO)
                    {
                        var npc = new MapNpc(npcDTO);
                        npc.Initialize(Session.CurrentMapInstance);
                        Session.CurrentMapInstance.AddNPC(npc);
                        Session.CurrentMapInstance?.Broadcast(npc.GenerateIn());
                    }
                }

                    MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddNpcPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}