using ChickenAPI.Events;
using Frostvein.GameObject._BCards;
using Frostvein.GameObject._BCards.Event;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.BCard
{
    public class BCardEventHandler : GenericEventHandlerBase<BCardEvent>
    {
        private readonly IBCardEffectHandlerContainer _bCardEventHandler;

        public BCardEventHandler(IBCardEffectHandlerContainer itemUsageHandler)
        {
            _bCardEventHandler = itemUsageHandler;
        }

        protected override void Handle(BCardEvent e, CancellationToken cancellation)
        {
            _bCardEventHandler.Execute(e.Target, e.Sender, e.Card);
        }
    }
}