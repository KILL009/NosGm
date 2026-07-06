using System;

namespace Frostvein.GameObject.Extension
{
    public static class NumberExtension
    {
        public static bool CheckIfInRange(this long number, long firstNumber, long endNumber)
        {
            if (number < firstNumber || number > endNumber) return false;

            for (long i = firstNumber; i <= endNumber; i++)
            {
                if (number == i) return true;
            }

            return false;
        }

        public static bool CheckIfInRange(this int number, int firstNumber, int endNumber)
        {
            return Convert.ToInt64(number).CheckIfInRange(firstNumber, endNumber);
        }

        public static bool CheckIfInRange(this short number, short firstNumber, short endNumber)
        {
            return Convert.ToInt64(number).CheckIfInRange(firstNumber, endNumber);
        }

        public static bool CheckIfInRange(this byte number, byte firstNumber, byte endNumber)
        {
            return Convert.ToInt64(number).CheckIfInRange(firstNumber, endNumber);
        }
    }
}
