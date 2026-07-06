using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class ShopMapper
    {
        #region Methods

        public static bool ToShop(ShopDTO input, Shop output)
        {
            if (input == null) return false;

            output.MapNpcId = input.MapNpcId;
            output.MenuType = input.MenuType;
            output.Name = input.Name;
            output.ShopId = input.ShopId;
            output.ShopType = input.ShopType;

            return true;
        }

        public static bool ToShopDTO(Shop input, ShopDTO output)
        {
            if (input == null) return false;

            output.MapNpcId = input.MapNpcId;
            output.MenuType = input.MenuType;
            output.Name = input.Name;
            output.ShopId = input.ShopId;
            output.ShopType = input.ShopType;

            return true;
        }

        #endregion
    }
}