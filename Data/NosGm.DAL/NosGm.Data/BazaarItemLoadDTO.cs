using System;

namespace NosGm.Data
{
    [Serializable]
    public class BazaarItemLoadDTO
    {
        public BazaarItemDTO BazaarItem { get; set; }

        public ItemInstanceDTO ItemInstance { get; set; }

        public string OwnerName { get; set; }
    }
}
