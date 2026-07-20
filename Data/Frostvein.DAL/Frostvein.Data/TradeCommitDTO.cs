using System;
using System.Collections.Generic;

namespace Frostvein.Data
{
    public enum TradeCommitResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        Conflict = 2,
        MissingSchema = 3,
        Error = 4
    }

    [Serializable]
    public sealed class TradeCommitDTO
    {
        public TradeCommitDTO()
        {
            BeforeItems = new List<ItemInstanceDTO>();
            AfterItems = new List<ItemInstanceDTO>();
        }

        public Guid OperationId { get; set; }

        public long FirstCharacterId { get; set; }

        public long SecondCharacterId { get; set; }

        public long FirstGoldBefore { get; set; }

        public long FirstGoldAfter { get; set; }

        public long FirstGoldBankBefore { get; set; }

        public long FirstGoldBankAfter { get; set; }

        public long SecondGoldBefore { get; set; }

        public long SecondGoldAfter { get; set; }

        public long SecondGoldBankBefore { get; set; }

        public long SecondGoldBankAfter { get; set; }

        public List<ItemInstanceDTO> BeforeItems { get; set; }

        public List<ItemInstanceDTO> AfterItems { get; set; }
    }
}
