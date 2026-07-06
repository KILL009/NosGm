using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Event.Handler
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
