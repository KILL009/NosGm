namespace Frostvein.DAL.EF
{
    public class RecipeList
    {
        #region Properties

        public virtual Item Item { get; set; }

        public short? ItemVNum { get; set; }

        public virtual MapNpc MapNpc { get; set; }

        public int? MapNpcId { get; set; }

        public virtual Recipe Recipe { get; set; }

        public short RecipeId { get; set; }

        public int RecipeListId { get; set; }

        #endregion
    }
}