using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IShopSkillDAO
    {
        #region Methods

        ShopSkillDTO Insert(ShopSkillDTO shopSkill);

        void Insert(List<ShopSkillDTO> skills);

        IEnumerable<ShopSkillDTO> LoadAll();

        IEnumerable<ShopSkillDTO> LoadByShopId(int shopId);

        #endregion
    }
}