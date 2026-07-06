using Frostvein.Domain;
using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class ScriptedInstance
    {
        #region Properties

        public string Label { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public int QuestTimeSpaceId { get; set; }

        [MaxLength(int.MaxValue)] public string Script { get; set; }

        public short ScriptedInstanceId { get; set; }

        public ScriptedInstanceType Type { get; set; }

        public string Info { get; set; }

        #endregion
    }
}