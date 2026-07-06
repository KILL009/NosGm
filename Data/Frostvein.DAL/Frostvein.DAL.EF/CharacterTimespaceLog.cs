using System;
using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class CharacterTimespaceLog
    {
        [Key]
        public long LogId { get; set; }

        public long CharacterId { get; set; }

        public short ScriptedInstanceId { get; set; }

        public long Score { get; set; }

        public DateTime Date { get; set; }

        public bool IsFailed { get; set; }
    }
}
