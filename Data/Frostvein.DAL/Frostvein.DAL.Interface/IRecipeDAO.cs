using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IRecipeDAO
    {
        #region Methods

        RecipeDTO Insert(RecipeDTO recipe);

        IEnumerable<RecipeDTO> LoadAll();

        RecipeDTO LoadById(short recipeId);

        RecipeDTO LoadByItemVNum(short itemVNum);

        void Update(RecipeDTO recipe);

        #endregion
    }
}