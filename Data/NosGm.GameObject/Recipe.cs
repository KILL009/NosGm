using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class Recipe : RecipeDTO
    {
        #region Properties

        public List<RecipeItemDTO> Items { get; set; }

        #endregion

        #region Instantiation

        public Recipe()
        {
        }

        public Recipe(RecipeDTO input)
        {
            Amount = input.Amount;
            ItemVNum = input.ItemVNum;
            RecipeId = input.RecipeId;
        }

        #endregion
    }
}