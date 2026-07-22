using System;
using static NosGm.Domain.BCardType;

namespace NosGm.Data
{
    [Serializable]
    public class FairyEnchantmentDTO
    {
        public long FairyEnchantmentId { get; set; }

        public Guid EquipmentSerialId { get; set; }

        public CardType Type { get; set; }

        public byte SubType { get; set; }

        public int FirstData { get; set; }

        public int SecondData { get; set; }

        public int ThirdData { get; set; }

        public FairyEnchantmentDTO DeepCopy() => (FairyEnchantmentDTO)MemberwiseClone();
    }
}