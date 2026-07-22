using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class CheckEvent
    {
        public static async Task Load()
        {
            await Task.Run(() => CommunicationServiceClient.Instance.CheckForStuckAccountsAtSaving());
        }
    }
}
