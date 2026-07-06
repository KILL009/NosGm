using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IRecipeItemDAO
    {
        #region Methods

        RecipeItemDTO Insert(RecipeItemDTO recipeItem);

        IEnumerable<RecipeItemDTO> LoadAll();

        RecipeItemDTO LoadById(short recipeItemId);

        IEnumerable<RecipeItemDTO> LoadByRecipe(short recipeId);

        IEnumerable<RecipeItemDTO> LoadByRecipeAndItem(short recipeId, short itemVNum);

        #endregion
    }
}