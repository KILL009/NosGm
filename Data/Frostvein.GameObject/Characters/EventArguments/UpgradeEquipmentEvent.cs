using Frostvein.Domain;
using Frostvein.GameObject._Event;

namespace Frostvein.GameObject.EventArguments
{
    public class UpgradeEquipmentEvent : PlayerEvent
    {
        public UpgradeEquipmentEvent(Character character, ItemInstance item, UpgradeMode mode, UpgradeProtection protection)
            => (Character, Item, Mode, Protection) = (character, item, mode, protection);

        public Character Character { get; }

        public ItemInstance Item { get; }

        public UpgradeMode Mode { get; }

        public UpgradeProtection Protection { get; }
    }
}
