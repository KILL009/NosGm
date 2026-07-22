using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.TitanShield.Thread
{
    public static class DoNothingThread
    {
        public static async Task DoNothing()
        {
            await DoNothing();
        }
    }
}
