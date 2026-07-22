using System;
using System.ComponentModel.DataAnnotations;

namespace NosGm.Data
{
    [Serializable]
    public class BazaarItemDTO
    {
        #region Properties

        public long AccountId { get; set; }

        [MaxLength(45)]
        public string RegistrationIP { get; set; }

        public string CurrentIp { get; set; }

        public short Amount { get; set; }

        public long BazaarItemId { get; set; }

        public DateTime DateStart { get; set; }

        public short Duration { get; set; }

        public bool IsPackage { get; set; }

        public Guid ItemInstanceId { get; set; }

        public bool MedalUsed { get; set; }

        public long Price { get; set; }

        public long SellerId { get; set; }

        #endregion
    }
}