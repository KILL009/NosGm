using Frostvein.Domain;

namespace Frostvein.GameObject.Extension.Item
{
    public static class FusionItemExtension
    {
        #region Methods

        public static void FusionItem(this ItemInstance e, ClientSession Session, ItemInstance secondItem)
        {
            long gold = 1000000;

            if (Session.Character.Gold < gold)
            {
                return;
            }

            if (Session.Character.Inventory.CountItem(5874) < 1 && Session.Character.Inventory.CountItem(9112) < 1)
            {
                return;
            }

            if (e.Item.ItemSubType != secondItem.Item.ItemSubType)
            {
                return;
            }

            if (e.Item.ItemType != ItemType.Fashion)
            {
                return;
            }

            if (secondItem.Item.ItemType != ItemType.Fashion)
            {
                return;
            }

            if (e.Item.ItemSubType <= 3)
            {
                return;
            }

            if (secondItem.Item.ItemSubType <= 3)
            {
                return;
            }

            // C'mon ... Missing data on DB so hardcod this shit
            if (!e.Item.Name.ToLower().Contains("permanent"))
            {
                return;
            }

            if (!secondItem.Item.Name.ToLower().Contains("permanent"))
            {
                return;
            }

            Session.SendPacket("msg 0 Fusion Completed!");
            if (Session.Character.Inventory.CountItem(9112) > 0)
            {
                Session.Character.Inventory.RemoveItemAmount(9112);
            }
            else
            {
                Session.Character.Inventory.RemoveItemAmount(5874);
            }

            e.FusionVnum = secondItem.ItemVNum;
            Session.Character.DeleteItemByItemInstanceId(secondItem.Id);
            Session.Character.Gold -= gold;
            Session.SendPacket(Session.Character.GenerateGold());
        }

        #endregion
    }
}