using ChickenAPI.Events;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.Guri
{
    public class GuriEventHandler : GenericEventHandlerBase<GuriEvent>
    {
        private readonly IGuriHandlerContainer _guriHandler;

        public GuriEventHandler(IGuriHandlerContainer guriHandler) => _guriHandler = guriHandler;

        protected override void Handle(GuriEvent e, CancellationToken cancellation)
        {
            _guriHandler.Handle(e.Sender, e);
        }
    }
}