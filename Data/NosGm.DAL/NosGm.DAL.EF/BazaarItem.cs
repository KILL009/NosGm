using System;
using System.ComponentModel.DataAnnotations;

namespace NosGm.DAL.EF
{
    public class BazaarItem
    {
        #region Properties

        public long AccountId { get; set; }

        [MaxLength(45)]
        public string RegistrationIP { get; set; }

        public string CurrentIp { get; set; }

        public short Amount { get; set; }

        public long BazaarItemId { get; set; }

        public virtual Character Character { get; set; }

        public DateTime DateStart { get; set; }

        public short Duration { get; set; }

        public bool IsPackage { get; set; }

        public virtual ItemInstance ItemInstance { get; set; }

        public Guid ItemInstanceId { get; set; }

        public bool MedalUsed { get; set; }

        public long Price { get; set; }

        public long SellerId { get; set; }

        #endregion
    }
}