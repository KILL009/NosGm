using System;
using NosGm.Domain;

namespace NosGm.Algorithm
{
    internal class Constants
    {
        internal const byte MaxLevel = 99;
        internal const byte MaxFairyLevel = 80;
        internal const byte MaxJobLevel = 80;
        internal const byte MaxHeroLevel = 60;
        internal static readonly int ClassCount = Enum.GetNames(typeof(ClassType)).Length;
    }
}
