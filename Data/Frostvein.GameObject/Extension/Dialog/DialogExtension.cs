using System.Threading.Tasks;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Service
{
    public static class DialogExtension
    {
        public static async Task GenerateDialog(ClientSession Session, int DialogId)
        {
            Session.SendPacket($"npc_req 1 {Session.Character.CharacterId} {DialogId}");
        }
    }
}