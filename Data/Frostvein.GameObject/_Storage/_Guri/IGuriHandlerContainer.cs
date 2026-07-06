using Frostvein.GameObject._Event;
using Frostvein.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Frostvein.GameObject._Guri
{
    public interface IGuriHandlerContainer
    {
        Task Register(IGuriHandler handler);

        Task Unregister(long guriEffectId);

        void Handle(EventEntity player, GuriEvent args);
    }
}