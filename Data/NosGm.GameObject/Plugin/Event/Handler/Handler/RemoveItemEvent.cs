using NosGm.Core;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class RemoveItemEvent
    {
        public static void Remove()
        {
            try
            {
                foreach (var session in ServerManager.Instance.Sessions.Where(c => c.IsConnected))
                {
                    session.Character?.RefreshValidity();
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"[Error] {e}", Domain.LogType.ERROR);
            }
        }
    }
}
