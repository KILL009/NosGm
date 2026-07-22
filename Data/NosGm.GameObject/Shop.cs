using NosGm.Data;
using NosGm.GameObject.Networking;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class Shop : ShopDTO
    {
        #region Methods

        public void Initialize()
        {
            ShopItems = ServerManager.Instance.GetShopItemsByShopId(ShopId);
            ShopSkills = ServerManager.Instance.GetShopSkillsByShopId(ShopId);
        }

        #endregion

        #region Instantiation

        public Shop()
        {
        }

        public Shop(ShopDTO input)
        {
            MapNpcId = input.MapNpcId;
            MenuType = input.MenuType;
            Name = input.Name;
            ShopId = input.ShopId;
            ShopType = input.ShopType;
        }

        #endregion

        #region Properties

        public List<ShopItemDTO> ShopItems { get; set; }

        public List<ShopSkillDTO> ShopSkills { get; set; }

        #endregion
    }
}