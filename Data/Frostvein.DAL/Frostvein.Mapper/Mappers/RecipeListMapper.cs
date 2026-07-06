using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class RecipeListMapper
    {
        #region Methods

        public static bool ToRecipeList(RecipeListDTO input, RecipeList output)
        {
            if (input == null) return false;

            output.ItemVNum = input.ItemVNum;
            output.MapNpcId = input.MapNpcId;
            output.RecipeId = input.RecipeId;
            output.RecipeListId = input.RecipeListId;

            return true;
        }

        public static bool ToRecipeListDTO(RecipeList input, RecipeListDTO output)
        {
            if (input == null) return false;

            output.ItemVNum = input.ItemVNum;
            output.MapNpcId = input.MapNpcId;
            output.RecipeId = input.RecipeId;
            output.RecipeListId = input.RecipeListId;

            return true;
        }

        #endregion
    }
}