using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadBazaarItems
    {
        public static void Load()
        {
            try
            {
                ServerManager.Instance.BazaarList = new ThreadSafeGenericList<BazaarItemLink>();

                foreach (BazaarItemLoadDTO row in DAOFactory.BazaarItemDAO.LoadAllHydrated())
                {
                    ServerManager.Instance.BazaarList.Add(new BazaarItemLink
                    {
                        BazaarItem = row.BazaarItem,
                        Owner = row.OwnerName,
                        Item = row.ItemInstance == null ? null : new ItemInstance(row.ItemInstance)
                    });
                }

                LoggerService.LogServer.Logger.UpdateLoadOutput(
                    $"{ServerManager.Instance.BazaarList.Count} Bazaar Items - Status: Successful",
                    Domain.LogType.LOAD);
            }
            catch (Exception ex)
            {
                LoggerService.LogServer.Logger.LogAsync(ex.ToString(), Domain.LogType.ERROR);
                throw;
            }
        }
    }
}
