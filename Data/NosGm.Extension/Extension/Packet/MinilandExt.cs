using NosGm.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Extension.GameExtension.Packet
{
    public static class MinilandExt
    {
        #region Methods

        public static Gift GetMinilandGift(this short game, int point)
        {
            var gifts = new List<Gift>();
            var rand = new Random();
            switch (game)
            {
                case 3117:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(1031, 2));
                            gifts.Add(new Gift(1032, 2));
                            gifts.Add(new Gift(1033, 2));
                            gifts.Add(new Gift(1034, 2));
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2105, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3118:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2032, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2106, 1));
                            gifts.Add(new Gift(2038, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            gifts.Add(new Gift(2039, 2));
                            gifts.Add(new Gift(2109, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            gifts.Add(new Gift(2040, 2));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3119:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2027, 15));
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2046, 2));
                            gifts.Add(new Gift(2191, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2047, 2));
                            gifts.Add(new Gift(2191, 1));
                            gifts.Add(new Gift(2117, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2048, 2));
                            gifts.Add(new Gift(2191, 1));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3120:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2017, 10));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2042, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2043, 2));
                            gifts.Add(new Gift(2118, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2044, 2));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3121:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(1031, 3));
                            gifts.Add(new Gift(1032, 3));
                            gifts.Add(new Gift(1033, 3));
                            gifts.Add(new Gift(1034, 3));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2034, 3));
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2035, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(2275, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2036, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2037, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2197, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3122:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2032, 4));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2038, 3));
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2039, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(2105, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2040, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2041, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2198, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3123:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2047, 15));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2046, 3));
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2047, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(2117, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2048, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2049, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2199, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3124:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2017, 10));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2042, 3));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2043, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(2118, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2044, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2045, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2200, 1));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3125:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2034, 4));
                            gifts.Add(new Gift(2189, 2));
                            gifts.Add(new Gift(2205, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2035, 4));
                            gifts.Add(new Gift(2105, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2036, 4));
                            gifts.Add(new Gift(2193, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2037, 4));
                            gifts.Add(new Gift(2193, 2));
                            gifts.Add(new Gift(2201, 2));
                            gifts.Add(new Gift(2226, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2213, 1));
                            gifts.Add(new Gift(2193, 2));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2226, 2));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3126:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2038, 4));
                            gifts.Add(new Gift(2106, 2));
                            gifts.Add(new Gift(2206, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2039, 4));
                            gifts.Add(new Gift(2109, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2040, 4));
                            gifts.Add(new Gift(2194, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2040, 4));
                            gifts.Add(new Gift(2194, 2));
                            gifts.Add(new Gift(2201, 2));
                            gifts.Add(new Gift(2231, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2214, 1));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(2231, 2));
                            gifts.Add(new Gift(2202, 1));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3127:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2046, 4));
                            gifts.Add(new Gift(2207, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2047, 4));
                            gifts.Add(new Gift(2117, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2048, 4));
                            gifts.Add(new Gift(2195, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2049, 4));
                            gifts.Add(new Gift(2195, 2));
                            gifts.Add(new Gift(2199, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2216, 1));
                            gifts.Add(new Gift(2195, 2));
                            gifts.Add(new Gift(2203, 1));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3128:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                // GM mini-game
                case 3130:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;

                case 3131:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            gifts.Add(new Gift(1013, (byte)(point + 1)));
                            break;
                    }

                    break;
            }

            return gifts.OrderBy(s => rand.Next()).FirstOrDefault();
        }

        public static int[] GetMinilandMaxPoint(this byte game)
        {
            switch (game)
            {
                case 0:
                    return new[] { 999, 4999, 7999, 11999, 15999, 1000000 };

                case 1:
                    return new[] { 999, 4999, 9999, 13999, 17999, 1000000 };

                case 2:
                    return new[] { 999, 3999, 7999, 14999, 24999, 1000000 };

                case 3:
                    return new[] { 999, 3999, 7999, 11999, 19999, 1000000 };

                case 4:
                case 5:
                    return new[] { 999, 4999, 7999, 11999, 15999, 1000000 };

                default:
                    return new[] { 999, 4999, 7999, 14999, 24999, 1000000 };
            }
        }

        #endregion
    }
}