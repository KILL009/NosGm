using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.GameObject;
using NosGm.GameObject.Battle.Thread;
using NosGm.GameObject.Discord;
using NosGm.GameObject.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class TestHandler : IPacketHandler
    {
        #region Instantiation

        public TestHandler(ClientSession session) => Session = session;

        public List<CharacterTimespaceLogDTO> TimespaceLogs2 = new();

        public CharacterQuestDTO characterQuest2 { get; set; }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Test(TestCommandPacket testPacket)
        {

            switch (testPacket.Type)
            {
                case 1:
                    //ServerManager.Instance.LoadBazaar();
                    break;

                case 2:
                    await Discord.SendEmbed("Title", "FirstContext", $"SecondContext", "");
                    break;
            }
        }

        #endregion
    }
}