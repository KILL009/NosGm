using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class AddQuestHandler : IPacketHandler
    {
        #region Instantiation

        public AddQuestHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void AddQuest(AddQuestPacket addQuestPacket)
        {
            if (addQuestPacket != null)
            {
                if (ServerManager.Instance.Quests.Any(q => q.QuestId == addQuestPacket.QuestId))
                {
                    Session.Character.AddQuest(addQuestPacket.QuestId);
                    return;
                }

                Session.SendPacket(Session.Character.GenerateSay("This Quest doesn't exist", 11));
            }
        }

        #endregion
    }
}