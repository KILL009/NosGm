using NosGm.Core;
using NosGm.DAL;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class SaveEvent
    {
        
        public static async Task Save()
        {
            try
            {
                await SaveAll();
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"[Error] {e}", Domain.LogType.ERROR);
            }
        }

        public static async Task SaveAll()
        {
            await Task.WhenAll(ServerManager.Instance.Sessions.Select(async sess =>
            {
                await sess.Character.Event.EmitEventAsync(new CharacterSaveEvent());
            }));
            DAOFactory.BazaarItemDAO.RemoveOutDated();
        }
    }
}
