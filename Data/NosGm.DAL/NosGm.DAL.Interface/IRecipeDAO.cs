using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IRecipeDAO
    {
        #region Methods

        RecipeDTO Insert(RecipeDTO recipe);

        IEnumerable<RecipeDTO> LoadAll();

        CacheStatisticsSnapshot GetCacheStatistics();

        RecipeDTO LoadById(short recipeId);

        RecipeDTO LoadByItemVNum(short itemVNum);

        void Update(RecipeDTO recipe);

        #endregion
    }
}