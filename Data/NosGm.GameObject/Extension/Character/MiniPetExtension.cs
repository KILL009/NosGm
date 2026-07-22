using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.XMLModel.Objects;
using System.Threading.Tasks;

namespace NosGm.GameObject.Extension.Reputation
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
