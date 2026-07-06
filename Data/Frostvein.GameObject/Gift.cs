namespace Frostvein.GameObject
{
    public class Gift
    {
        #region Instantiation

        public Gift(short vnum, short amount, short design = 0, bool isRareRandom = true, bool isHeroic = false)
        {
            VNum = vnum;
            Amount = amount;
            IsRandomRare = isRareRandom;
            Design = design;
            IsHeroic = isHeroic;
        }

        #endregion

        #region Properties

        public short Amount { get; set; }

        public short Design { get; set; }

        public bool IsRandomRare { get; set; }

        public byte MaxTeamSize { get; set; }

        public byte MinTeamSize { get; set; }

        public short VNum { get; set; }

        public bool IsHeroic { get; set; }

        #endregion
    }
}