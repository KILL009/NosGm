using System.Collections.Generic;

namespace Frostvein.DAL.EF
{
    public class Recipe
    {
        #region Instantiation

        public Recipe()
        {
            RecipeItem = new HashSet<RecipeItem>();
            RecipeList = new HashSet<RecipeList>();
        }

        #endregion

        #region Properties

        public short Amount { get; set; }

        public virtual Item Item { get; set; }

        public short ItemVNum { get; set; }

        public short RecipeId { get; set; }

        public virtual ICollection<RecipeItem> RecipeItem { get; set; }

        public virtual ICollection<RecipeList> RecipeList { get; set; }

        #endregion
    }
}