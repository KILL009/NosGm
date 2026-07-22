using System;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class ExchangeInfo
    {
        #region Instantiation

        public ExchangeInfo()
        {
            Confirmed = false;
            Gold = 0;
            GoldBank = 0;
            TargetCharacterId = -1;
            ExchangeList = new List<ItemInstance>();
            Validated = false;
            OperationId = Guid.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Legacy alias kept for packet compatibility. New trade code uses GoldBank.
        /// </summary>
        public long BankGold
        {
            get => GoldBank;
            set => GoldBank = value;
        }

        public bool CommitStarted { get; set; }

        public bool Confirmed { get; set; }

        public List<ItemInstance> ExchangeList { get; set; }

        public long Gold { get; set; }

        public long GoldBank { get; set; }

        /// <summary>
        /// Stable identifier shared by both participants for idempotent persistence,
        /// auditing and item-trace events.
        /// </summary>
        public Guid OperationId { get; set; }

        public long TargetCharacterId { get; set; }

        public bool Validated { get; set; }

        #endregion
    }
}
