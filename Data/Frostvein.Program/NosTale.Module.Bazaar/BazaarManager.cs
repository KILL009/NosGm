using Microsoft.Extensions.Hosting;
using Frostvein.Core.Threading;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frostvein.GameObject.Plugin.Load;

namespace NosTale.Module.Bazaar
{
    public class BazaarManager
    {
        private readonly ConcurrentDictionary<long, object> _itemLocks =
            new ConcurrentDictionary<long, object>();

        public BazaarManager()
        {
            BazaarItems = new ThreadSafeLockedDictionary<long, BazaarItemDTO>();
            BazaarItemLinks = new ThreadSafeLockedDictionary<long, BazaarItemLink>();
            BazaarItemStates = new ConcurrentBag<long>();
        }

        public ThreadSafeLockedDictionary<long, BazaarItemDTO> BazaarItems { get; set; }

        public ThreadSafeLockedDictionary<long, BazaarItemLink> BazaarItemLinks { get; set; }

        public ConcurrentBag<long> BazaarItemStates { get; set; }

        public object GetItemLock(long bazaarItemId) =>
            _itemLocks.GetOrAdd(bazaarItemId, _ => new object());

        public void Initialize()
        {
            LoadBazaarItemsAsync();
        }

        public void LoadBazaarItemsAsync()
        {
            PluginLoadItems.Load();

            var bazaarItems = DAOFactory.BazaarItemDAO.LoadAll();

            if (bazaarItems?.Any() != true)
            {
                Console.WriteLine("No bazaar items loaded.");
                return;
            }

            var dictionary = bazaarItems.ToDictionary(x => x.BazaarItemId, y => y);
            BazaarItems = new ThreadSafeLockedDictionary<long, BazaarItemDTO>(dictionary);
            Console.WriteLine($"{BazaarItems.Count} Bazaar Items loaded.");

            var partitioner = Partitioner.Create(bazaarItems, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, bz =>
            {
                BazaarItemLinks.TryAdd(bz.BazaarItemId, new BazaarItemLink
                {
                    BazaarItem = bz,
                    Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId)),
                    Owner = DAOFactory.CharacterDAO.LoadById(bz.SellerId)?.Name
                });
            });

            Console.WriteLine($"{BazaarItemLinks.Count} Bazaar item links created.");
        }
    }
}
