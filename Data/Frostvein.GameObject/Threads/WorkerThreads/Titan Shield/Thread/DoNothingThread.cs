using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.TitanShield.Thread
{
    public static class DoNothingThread
    {
        public static async Task DoNothing()
        {
            await DoNothing();
        }
    }
}
