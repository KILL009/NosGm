using ChickenAPI.Events;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.Characters
{
    public class ChangeSexEventHandler : GenericEventHandlerBase<ChangeSexEvent>
    {
        protected override void Handle(ChangeSexEvent e, CancellationToken cancellation)
        {
            Character character = e.Sender.Character;
            HandleChangeSex(character);
        }

        private void HandleChangeSex(Character character)
        {
            character.Gender = character.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;
            if (character.IsVehicled)
            {
                character.Morph = character.Gender == GenderType.Female ? character.Morph + 1 : character.Morph - 1;
            }

            character.Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SEX_CHANGED"), 0));
            character.Session.SendPacket(character.GenerateEq());
            character.Session.SendPacket(character.GenerateGender());
            character.Session.CurrentMapInstance?.Broadcast(character.Session, character.GenerateIn(), ReceiverType.AllExceptMe);
            character.Session.CurrentMapInstance?.Broadcast(character.Session, character.GenerateGidx(), ReceiverType.AllExceptMe);
            character.Session.CurrentMapInstance?.Broadcast(character.GenerateCMode());
            character.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, character.CharacterId, 196),character.PositionX, character.PositionY);
        }
    }
}
