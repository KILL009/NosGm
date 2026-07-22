using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class NpcToSummon
    {
        #region Instantiation

        public NpcToSummon(short vnum, MapCell spawnCell, long target, bool isProtected = false, bool isMate = false,
            bool move = false, bool isHostile = false, bool isTsReward = false)
        {
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsProtected = isProtected;
            IsHostile = isHostile;
            IsMate = isMate;
            IsTsReward = isTsReward;
            Move = move;
            DeathEvents = new List<EventContainer>();
            SpawnEvents = new List<EventContainer>();
        }

        #endregion

        #region Properties

        public List<EventContainer> DeathEvents { get; set; }

        public byte Dir { get; set; }

        public bool IsHostile { get; set; }

        public bool IsMate { get; set; }

        public bool IsProtected { get; set; }

        public bool IsTsReward { get; set; }

        public bool Move { get; set; }

        public MapCell SpawnCell { get; set; }

        public List<EventContainer> SpawnEvents { get; set; }

        public long Target { get; set; }

        public short VNum { get; set; }

        #endregion
    }
}