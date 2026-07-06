using System;

namespace Frostvein.Data
{
    [Serializable]
    public class MapNpcDTO
    {
        #region Properties

        public short Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public short MapId { get; set; }

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public string Name { get; set; }

        public short NpcVNum { get; set; }

        public byte Position { get; set; }

        public string Say { get; set; }

        public int Delay { get; set; }

        public string Information { get; set; }

        #endregion
    }
}