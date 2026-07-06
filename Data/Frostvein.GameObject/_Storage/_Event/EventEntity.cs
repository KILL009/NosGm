using ChickenAPI.Events;
using System.Threading.Tasks;

namespace Frostvein.GameObject._Event
{
    public class EventEntity
    {
        public EventEntity(Character e)
        {
            Character = e;
        }

        public EventEntity(ClientSession e)
        {
            Session = e;
        }

        public EventEntity(Mate e)
        {
            Mate = e;
        }

        public EventEntity(MapNpc e)
        {
            MapNpc = e;
        }

        public EventEntity(MapMonster e)
        {
            MapMonster = e;
        }

        public Character Character { get; set; }

        public ClientSession Session { get; set; }

        public MapNpc MapNpc { get; set; }

        public MapMonster MapMonster { get; set; }

        public Mate Mate { get; set; }

        private static IEventPipeline _eventPipeline;

        public static void InitializeEventPipeline(IEventPipeline eventPipeline)
        {
            _eventPipeline = eventPipeline;
        }

        public void EmitEvent<T>(T e) where T : PlayerEvent
        {
            e.Sender = this;
            EmitEventAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task EmitEventAsync<T>(T e) where T : PlayerEvent
        {
            e.Sender = this;
            await _eventPipeline.Notify(e).ConfigureAwait(false);
        }
    }
}