using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadBazaarItems
    {
        public static void Load()
        {
            try
            {
                ServerManager.Instance.BazaarList = new ThreadSafeGenericList<BazaarItemLink>();
                OrderablePartitioner<BazaarItemDTO> bazaarPartitioner = Partitioner.Create(DAOFactory.BazaarItemDAO.LoadAll(), EnumerablePartitionerOptions.NoBuffering);
                Parallel.ForEach(bazaarPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, bazaarItem =>
                {
                    BazaarItemLink item = new BazaarItemLink
                    {
                        BazaarItem = bazaarItem
                    };
                    CharacterDTO chara = DAOFactory.CharacterDAO.LoadById(bazaarItem.SellerId);
                    if (chara != null)
                    {
                        item.Owner = chara.Name;
                        item.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bazaarItem.ItemInstanceId));
                    }
                    ServerManager.Instance.BazaarList.Add(item);
                });
                LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.BazaarList.Count} Bazaar Items - Status: Successful", Domain.LogType.LOAD);
            }
            catch (Exception ex)
            {
                LoggerService.LogServer.Logger.LogAsync($"{ex.ToString()}", Domain.LogType.ERROR);
                throw;
            }
        }
    }
}
