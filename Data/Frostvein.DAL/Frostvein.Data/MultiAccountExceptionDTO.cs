using System;

namespace Frostvein.Data
{
    [Serializable]
    public class MultiAccountExceptionDTO
    {
        public long AccountId { get; set; }

        public long ExceptionId { get; set; }

        public byte ExceptionLimit { get; set; }
    }
}
