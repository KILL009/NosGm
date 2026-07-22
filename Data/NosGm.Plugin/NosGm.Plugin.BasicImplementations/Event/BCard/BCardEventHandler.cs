using ChickenAPI.Events;
using NosGm.GameObject._BCards;
using NosGm.GameObject._BCards.Event;
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