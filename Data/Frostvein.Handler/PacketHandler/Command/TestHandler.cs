using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle.Thread;
using Frostvein.GameObject.Discord;
using Frostvein.GameObject.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
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