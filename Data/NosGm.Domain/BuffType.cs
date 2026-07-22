using System;

namespace NosGm.Domain
{
    [Flags]
    public enum BuffType : byte
    {
        Good = 0,
        Neutral = 1,
        Bad = 2,
        All = Good | Neutral | Bad,
        Basic = Good | Neutral
    }
}