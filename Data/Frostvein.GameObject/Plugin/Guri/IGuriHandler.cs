using Frostvein.GameObject;
using Game.Configuration.Guri;
using System.Threading.Tasks;

namespace Game.Configuration.BCards
{
    public interface IGuriHandler
    {
        long GuriEffectId { get; }

        Task ExecuteAsync(ClientSession player, GuriEvent e);
    }
}