using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
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