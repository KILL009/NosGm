using ChickenAPI.Events;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events.Bank;
using Frostvein.GameObject.Helpers;
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
