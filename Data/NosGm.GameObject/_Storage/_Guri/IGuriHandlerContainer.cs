using NosGm.GameObject._Event;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace NosGm.GameObject._Guri
{
    public interface IGuriHandlerContainer
    {
        Task Register(IGuriHandler handler);

        Task Unregister(long guriEffectId);

        void Handle(EventEntity player, GuriEvent args);
    }
}