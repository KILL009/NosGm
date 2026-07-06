using System;

namespace Frostvein.Data
{
    [Serializable]
    public class RecipeItemDTO
    {
        #region Properties

        public short Amount { get; set; }

        public short ItemVNum { get; set; }

        public short RecipeId { get; set; }

        public short RecipeItemId { get; set; }

        #endregion
    }
}