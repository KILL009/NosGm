using Frostvein.Extension.GameExtension.Character;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events.Bank;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class BankHandler : IPacketHandler
    {
        #region Instantiation

        public BankHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void BankManagement(BankPacket bankPacket)
        {
            if (Session.Account.IsLimited)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                return;
            }
            Session.Character.Event.EmitEvent(new OpenBankEvent());
        }

        #endregion
    }
}