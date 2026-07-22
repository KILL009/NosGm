using NosGm.GameObject._Event;

namespace NosGm.GameObject._Guri.Event
{
    public class GuriEvent : PlayerEvent
    {
        public long Type { get; set; }

        public int Argument { get; set; }

        public long Parameter { get; set; }

        public int Data { get; set; }

        public long User { get; set; }

        public string Value { get; set; }
    }
}