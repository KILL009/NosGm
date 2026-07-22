using NosGm.Extension.GameExtension.Character;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events.Bank;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
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