using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Extension.Data
{
    public static class RandomNumberExtension
    {
        private static readonly Random random = new Random();
        public static int Generate(int min = 0, int max = 100)
        {
            return random.Next(min, max);
        }
    }
}
