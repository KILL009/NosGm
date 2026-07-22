using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace NosGm.GameObject._Guri
{
    public interface IGuriHandler
    {
        long GuriEffectId { get; }

        void Execute(ClientSession player, GuriEvent e);
    }
}