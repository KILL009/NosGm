using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject.Items
{
    public static class VNum14005
    {
        public static async Task Execute(ClientSession session, ItemInstance itemInstance, Item items)
        {

            string String = $"{itemInstance.Item.Name}\n\n";
            foreach (RaidboxDTO item in DAOFactory.RaidboxDAO.LoadByItemVNumAndDesign(itemInstance.ItemVNum, itemInstance.Design))
            {
                Item ite = ServerManager.GetItem(item.ItemGeneratedVNum);
                String += $"x{item.ItemGeneratedAmount} - {ite.Name}\n";
            }

            session.SendPacket(UserInterfaceHelper.GenerateModal(String, 1));
        }
    }
}
