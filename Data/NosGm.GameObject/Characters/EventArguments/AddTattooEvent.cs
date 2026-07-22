using NosGm.GameObject._Event;

namespace NosGm.GameObject.EventArguments
{
    public class AddTattooEvent : PlayerEvent
    {
        public AddTattooEvent(Character character, ItemInstance item) => (Character, Item) = (character, item);

        public Character Character { get; }

        public ItemInstance Item { get; }
    }
}
