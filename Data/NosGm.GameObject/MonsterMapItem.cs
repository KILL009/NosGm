using NosGm.Domain;
using NosGm.GameObject.Extension.Inventory;

namespace NosGm.GameObject
{
    public class MonsterMapItem : MapItem
    {
        #region Instantiation

        public MonsterMapItem(short x, short y, short itemVNum, int amount = 1, long ownerId = -1) : base(x, y)
        {
            ItemVNum = itemVNum;
            if (amount < 1000)
            {
                Amount = (short)amount;
            }

            GoldAmount = amount;
            OwnerId = ownerId;
        }

        #endregion

        #region Properties

        public sealed override short Amount { get; set; }

        public int GoldAmount { get; }

        public sealed override short ItemVNum { get; set; }

        public long? OwnerId { get; }

        #endregion

        #region Methods

        public override ItemInstance GetItemInstance()
        {
            if (_itemInstance == null && OwnerId != null)
            {
                _itemInstance = Inventory.InstantiateItemInstance(ItemVNum, OwnerId.Value, Amount);
            }

            return _itemInstance;
        }

        public void Rarify(ClientSession session)
        {
            var instance = GetItemInstance();
            if (instance?.Item?.Type == InventoryType.Equipment &&
                (instance?.Item?.ItemType == ItemType.Weapon || instance?.Item?.ItemType == ItemType.Armor))
            {
                instance?.RarifyItem(session, RarifyMode.Drop, RarifyProtection.None);
            }
        }

        #endregion
    }
}