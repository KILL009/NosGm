using Frostvein.Data;
using Frostvein.Domain;

namespace Frostvein.GameObject
{
    public class MapDesignObject : MinilandObjectDTO
    {
        #region Members

        public ItemInstance ItemInstance;

        #endregion

        #region Methods

        public string GenerateEffect(bool removed) => $"eff_g {ItemInstance.Item?.EffectValue ?? ItemInstance.Design} {MapX.ToString("00")}{MapY.ToString("00")} {MapX} {MapY} {(removed ? 1 : 0)}";

        public string GenerateMapDesignObject(bool deleted) => $"mlobj {(deleted ? 0 : 1)} {ItemInstance.Slot} {MapX} {MapY} {ItemInstance.Item.Width} {ItemInstance.Item.Height} 0 {ItemInstance.DurabilityPoint} 0 {(ItemInstance.Item.ItemType == ItemType.House ? 1 : 0)}";

        public string GenerateMinilandEffect(bool removed) => $"eff_g {ItemInstance.Design} {MapX.ToString("00")}{MapY.ToString("00")} {MapX} {MapY} {(removed ? 1 : 0)}";

        #endregion
    }

    public class MinilandObject : MinilandObjectDTO
    {
        #region Members

        public ItemInstance ItemInstance;

        #endregion

        #region Instantiation

        public MinilandObject()
        {
        }

        public MinilandObject(MinilandObjectDTO input)
        {
            CharacterId = input.CharacterId;
            ItemInstanceId = input.ItemInstanceId;
            Level1BoxAmount = input.Level1BoxAmount;
            Level2BoxAmount = input.Level2BoxAmount;
            Level3BoxAmount = input.Level3BoxAmount;
            Level4BoxAmount = input.Level4BoxAmount;
            Level5BoxAmount = input.Level5BoxAmount;
            MapX = input.MapX;
            MapY = input.MapY;
            MinilandObjectId = input.MinilandObjectId;
        }

        #endregion

        #region Methods

        public string GenerateMinilandEffect(bool removed) => $"eff_g {ItemInstance.Item.EffectValue} {MapX.ToString("00")}{MapY.ToString("00")} {MapX} {MapY} {(removed ? 1 : 0)}";

        public string GenerateMinilandObject(bool deleted) => $"mlobj {(deleted ? 0 : 1)} {ItemInstance.Slot} {MapX} {MapY} {ItemInstance.Item.Width} {ItemInstance.Item.Height} 0 {ItemInstance.DurabilityPoint} 0 {(ItemInstance.Item.IsMinilandObject ? 1 : 0)}";

        #endregion
    }
}