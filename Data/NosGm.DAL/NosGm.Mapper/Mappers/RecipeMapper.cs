using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class RecipeMapper
    {
        #region Methods

        public static bool ToRecipe(RecipeDTO input, Recipe output)
        {
            if (input == null) return false;

            output.Amount = input.Amount;
            output.ItemVNum = input.ItemVNum;
            output.RecipeId = input.RecipeId;

            return true;
        }

        public static bool ToRecipeDTO(Recipe input, RecipeDTO output)
        {
            if (input == null) return false;

            output.Amount = input.Amount;
            output.ItemVNum = input.ItemVNum;
            output.RecipeId = input.RecipeId;

            return true;
        }

        #endregion
    }
}