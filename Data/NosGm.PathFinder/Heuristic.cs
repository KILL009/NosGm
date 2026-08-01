using System;

namespace NosGm.PathFinder
{
    public static class Heuristic
    {
        private const double SQRT_2 = 1.4142135623730950488016887242097;

        public static double Chebyshev(int dx, int dy)
        {
            return Math.Max(dx, dy);
        }

        public static double Euclidean(int dx, int dy)
        {
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Manhattan(int dx, int dy)
        {
            return dx + dy;
        }

        public static double Octile(int dx, int dy)
        {
            return Math.Max(dx, dy) + (SQRT_2 - 1) * Math.Min(dx, dy);
        }
    }
}
