using NosGm.GameObject.Battle;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class MonsterToSummon
    {
        #region Instantiation

        public MonsterToSummon(short vnum, MapCell spawnCell, BattleEntity target, bool move, bool isTarget = false, bool isBonus = false, bool isHostile = true, bool isBoss = false, BattleEntity owner = null, int aliveTime = 0, int aliveTimeMp = 0, byte noticeRange = 0, short hasDelay = 0, int maxHp = 0, int maxMp = 0)
        {
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsMoving = move;
            IsTarget = isTarget;
            IsBonus = isBonus;
            IsBoss = isBoss;
            IsHostile = isHostile;
            DeathEvents = new List<EventContainer>();
            NoticingEvents = new List<EventContainer>();
            UseSkillOnDamage = new List<UseSkillOnDamage>();
            SpawnEvents = new List<EventContainer>();
            AfterSpawnEvents = new List<EventContainer>();
            Owner = owner;
            AliveTime = aliveTime;
            AliveTimeMp = aliveTimeMp;
            NoticeRange = noticeRange;
            HasDelay = hasDelay;
            MaxHp = maxHp;
            MaxMp = maxMp;
        }

        public MonsterToSummon(short vnum, MapCell spawnCell, BattleEntity target, bool move, int aliveTime, BattleEntity owner)
        {
            bool IsTarget = false;
            bool IsBonus = false;
            bool IsHostile = true;
            bool IsBoss = false;
            int AliveTimeMp = 0;
            byte NoticeRange = 0;
            short HasDelay = 0;
            int MaxHp = 0;
            int MaxMp = 0;
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsMoving = move;
            DeathEvents = new List<EventContainer>();
            NoticingEvents = new List<EventContainer>();
            UseSkillOnDamage = new List<UseSkillOnDamage>();
            SpawnEvents = new List<EventContainer>();
            AfterSpawnEvents = new List<EventContainer>();
            Owner = owner;
            AliveTime = aliveTime;
        }

        public MonsterToSummon(short vnum, MapCell spawnCell, BattleEntity target, bool move, bool isHostile, BattleEntity owner, int aliveTime)
        {
            bool IsTarget = false;
            bool IsBonus = false;
            bool IsBoss = false;
            int AliveTimeMp = 0;
            byte NoticeRange = 0;
            short HasDelay = 0;
            int MaxHp = 0;
            int MaxMp = 0;
            IsHostile = isHostile;
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsMoving = move;
            IsHostile = isHostile;
            DeathEvents = new List<EventContainer>();
            NoticingEvents = new List<EventContainer>();
            UseSkillOnDamage = new List<UseSkillOnDamage>();
            SpawnEvents = new List<EventContainer>();
            AfterSpawnEvents = new List<EventContainer>();
            Owner = owner;
        }

        public MonsterToSummon(short vnum, MapCell spawnCell, BattleEntity target, bool move, short hasDelay)
        {
            BattleEntity Owner = null;
            int AliveTime = 0;
            bool IsTarget = false;
            bool IsBonus = false;
            bool IsBoss = false;
            bool IsHostile = true;
            int AliveTimeMp = 0;
            byte NoticeRange = 0;
            int MaxHp = 0;
            int MaxMp = 0;
            HasDelay = hasDelay;
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsMoving = move;
            DeathEvents = new List<EventContainer>();
            NoticingEvents = new List<EventContainer>();
            UseSkillOnDamage = new List<UseSkillOnDamage>();
            SpawnEvents = new List<EventContainer>();
            AfterSpawnEvents = new List<EventContainer>();

        }

        public MonsterToSummon(short vnum, MapCell spawnCell, BattleEntity target, bool move, BattleEntity owner, short hasDelay)
        {
            int AliveTime = 0;
            bool IsTarget = false;
            bool IsBonus = false;
            bool IsBoss = false;
            bool IsHostile = true;
            int AliveTimeMp = 0;
            byte NoticeRange = 0;
            int MaxHp = 0;
            int MaxMp = 0;
            Owner = owner;
            HasDelay = hasDelay;
            VNum = vnum;
            SpawnCell = spawnCell;
            Target = target;
            IsMoving = move;
            DeathEvents = new List<EventContainer>();
            NoticingEvents = new List<EventContainer>();
            UseSkillOnDamage = new List<UseSkillOnDamage>();
            SpawnEvents = new List<EventContainer>();
            AfterSpawnEvents = new List<EventContainer>();
        }

        #endregion

        #region Properties

        public List<EventContainer> AfterSpawnEvents { get; set; }

        public int AliveTime { get; set; }

        public int AliveTimeMp { get; set; }

        public short Damage { get; set; }

        public List<EventContainer> DeathEvents { get; set; }

        public float HasDelay { get; set; }

        public bool IsBonus { get; set; }

        public bool IsBoss { get; set; }

        public bool IsHostile { get; set; }

        public bool IsMeteorite { get; set; }

        public bool IsMoving { get; set; }

        public bool IsTarget { get; set; }

        public bool IsVessel { get; set; }

        public int MaxHp { get; set; }

        public int MaxMp { get; set; }

        public byte NoticeRange { get; internal set; }

        public List<EventContainer> NoticingEvents { get; set; }

        public BattleEntity Owner { get; set; }

        public MapCell SpawnCell { get; set; }

        public List<EventContainer> SpawnEvents { get; set; }

        public BattleEntity Target { get; set; }

        public List<UseSkillOnDamage> UseSkillOnDamage { get; set; }

        public short VNum { get; set; }

        #endregion
    }
}