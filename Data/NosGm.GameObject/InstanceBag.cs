using System.Collections.Concurrent;

namespace NosGm.GameObject
{
    public class InstanceBag
    {
        #region Instantiation

        public InstanceBag()
        {
            Clock = new Clock(1);
            DeadList = new ConcurrentBag<long>();
            ButtonLocker = new Locker();
            MonsterLocker = new Locker();
        }

        #endregion

        #region Methods

        public string GenerateScore() => $"rnsc {Point}";

        #endregion

        #region Properties

        public Locker ButtonLocker { get; set; }

        public Clock Clock { get; set; }

        public int Combo { get; set; }

        public long CreatorId { get; set; }

        public ConcurrentBag<long> DeadList { get; set; }

        public byte EndState { get; set; }

        public int LaurenaRound { get; set; }

        public short Lives { get; set; }

        public bool Lock { get; set; }

        public Locker MonsterLocker { get; set; }

        public int MonstersKilled { get; set; }

        public int NpcsKilled { get; set; }

        public int Point { get; set; }

        public int RoomsVisited { get; set; }

        #endregion
    }
}