using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;

namespace NosGm.GameObject.Extension
{
    public static class TitleExtension
    {
        public static void GenerateTitle(ClientSession session, GuriEvent guriPacket)
        {

            if (session.Character.Inventory.CountItem(guriPacket.Argument) < 1)
            {
                return;
            }

            var item = ServerManager.GetItem((short)guriPacket.Argument);

            if (item == null)
            {
                return;
            }

            if (item.ItemType != ItemType.Title)
            {
                return;
            }

            session.Character.Title.Add(new CharacterTitleDTO
            {
                CharacterId = session.Character.CharacterId,
                Stat = 1,
                TitleVnum = guriPacket.Argument
            });

            session.SendPacket($"info Title '{item.Name}' has been activated!");

            session.Character.Inventory.RemoveItemAmount(guriPacket.Argument);
            session.SendPacket(session.Character.GenerateTitle());
        }
    }
}