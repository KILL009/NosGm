using ChickenAPI.Events;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events.Bank;
using NosGm.GameObject.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.Characters.Bank
{
    public class OpenBankEventHandler : GenericEventHandlerBase<OpenBankEvent>
    {
        protected override void Handle(OpenBankEvent e, CancellationToken cancellation)
        {
            Character character = e.Sender.Character;

            HandleOpenBank(character);
        }

        private void HandleOpenBank(Character character)
        {
            character.Session.SendPacket(character.Session.Character.GenerateGb((byte)GoldBankPacketType.OpenBank));
            character.Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Information, Language.Instance.GetMessageFromKey("OPEN_BANK")));
        }
    }
}
