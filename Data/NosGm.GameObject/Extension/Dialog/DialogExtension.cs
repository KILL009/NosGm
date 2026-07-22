using System.Threading.Tasks;
using NosGm.GameObject.Networking;

namespace NosGm.GameObject.Service
{
    public static class DialogExtension
    {
        public static async Task GenerateDialog(ClientSession Session, int DialogId)
        {
            Session.SendPacket($"npc_req 1 {Session.Character.CharacterId} {DialogId}");
        }
    }
}