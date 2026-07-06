namespace Frostvein.GameObject
{
    public class CharacterMapItem : MapItem
    {
        #region Instantiation

        public CharacterMapItem(short x, short y, ItemInstance itemInstance) : base(x, y) => ItemInstance = itemInstance;

        #endregion

        #region Methods

        public override ItemInstance GetItemInstance() => ItemInstance;

        #endregion

        #region Properties

        public override short Amount
        {
            get => ItemInstance.Amount;
            set => ItemInstance.Amount = Amount;
        }

        public ItemInstance ItemInstance { get; set; }

        public override short ItemVNum
        {
            get => ItemInstance.ItemVNum;
            set => ItemInstance.ItemVNum = value;
        }

        #endregion
    }
}