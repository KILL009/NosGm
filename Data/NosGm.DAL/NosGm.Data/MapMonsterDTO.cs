using System;

namespace NosGm.Data
{
    [Serializable]
    public class MapMonsterDTO
    {
        #region Properties

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public short MapId { get; set; }

        public int MapMonsterId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short MonsterVNum { get; set; }

        public string Name { get; set; }

        public byte Position { get; set; }

        public int Icon { get; set; }

        #endregion
    }
}