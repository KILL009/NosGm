using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using Frostvein.XMLModel.Objects;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension.Reputation
{
    public static class StatusExtension
    {
       public static void GenerateStatus(ClientSession Session, string Message)
       {
            if (Session.Character.SetStatus)
            {
                if (Session.Character.StatusMessage != null)
                {
                    Session.CurrentMapInstance?.Broadcast(Session, $"shop 1 {Session.Character.CharacterId} 1 3 0 {Message}");
                }
            }
       } 

       public static void RemoveStatus(ClientSession Session)
       {
            Session.Character.SetStatus = false;
            Session.Character.StatusMessage = null;
            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
       }
    }
}
