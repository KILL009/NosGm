using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Frostvein.DAL.EF
{
    public class MapNpc
    {
        #region Instantiation

        public MapNpc()
        {
            Shop = new HashSet<Shop>();
            Teleporter = new HashSet<Teleporter>();
            RecipeList = new HashSet<RecipeList>();
        }

        #endregion

        #region Properties

        public short Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public string Name { get; set; }

        public virtual NpcMonster NpcMonster { get; set; }

        public short NpcVNum { get; set; }

        public byte Position { get; set; }

        public string Say { get; set; }
        public int Delay { get; set; }

        public string Information { get; set; }

        public virtual ICollection<RecipeList> RecipeList { get; set; }

        public virtual ICollection<Shop> Shop { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }


        #endregion
    }
}