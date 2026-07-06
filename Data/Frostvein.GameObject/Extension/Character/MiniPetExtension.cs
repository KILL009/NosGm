using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using Frostvein.XMLModel.Objects;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension.Reputation
{
    public static class MiniPetExtension
    {
        public static void GenerateMiniPet(ClientSession Session)
        {
            if (Session.Character.MiniPet != 0)
            {
                Session.CurrentMapInstance?.Broadcast(Session, $"minipet 1 {Session.Character.CharacterId} {Session.Character.MiniPet}");
            }
        }
    }
}
