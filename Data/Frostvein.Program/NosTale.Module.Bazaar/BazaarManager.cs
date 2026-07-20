using Frostvein.Core.Threading;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            LoadBazaarItems();
        }

        public void LoadBazaarItems()
        {
            PluginLoadItems.Load();

            var bazaarItems = DAOFactory.BazaarItemDAO.LoadAll()?.ToList() ?? new List<BazaarItemDTO>();
            var validItems = new Dictionary<long, BazaarItemDTO>();
            var validLinks = new Dictionary<long, BazaarItemLink>();
            int orphanedListings = 0;

            foreach (BazaarItemDTO listing in bazaarItems)
            {
                if (!TryBuildLink(listing, out BazaarItemLink link, out string failure))
                {
                    orphanedListings++;
                    Console.WriteLine($"Skipping invalid bazaar listing {listing?.BazaarItemId}: {failure}");
                    continue;
                }

                validItems[listing.BazaarItemId] = listing;
                validLinks[listing.BazaarItemId] = link;
            }

            BazaarItems = new ThreadSafeLockedDictionary<long, BazaarItemDTO>(validItems);
            BazaarItemLinks = new ThreadSafeLockedDictionary<long, BazaarItemLink>(validLinks);

            Console.WriteLine($"{BazaarItems.Count} Bazaar Items loaded.");
            Console.WriteLine($"{BazaarItemLinks.Count} Bazaar item links created.");
            if (orphanedListings > 0)
            {
                Console.WriteLine($"{orphanedListings} orphaned bazaar listings were ignored; run $BazaarAudit suspicious.");
            }
        }

        /// <summary>
        /// Reloads one committed listing and its ItemInstance from the same database used
        /// by the NosBazaar service, then replaces the cache entry under the listing lock.
        /// </summary>
        public bool TryRefreshListing(long bazaarItemId, out string failure)
        {
            failure = null;
            if (bazaarItemId <= 0)
            {
                failure = "Invalid BazaarItemId";
                return false;
            }

            lock (GetItemLock(bazaarItemId))
            {
                BazaarItemDTO listing = DAOFactory.BazaarItemDAO.LoadById(bazaarItemId);
                if (listing == null)
                {
                    RemoveListingFromCache(bazaarItemId);
                    failure = "The committed listing could not be loaded from the service database";
                    return false;
                }

                if (!TryBuildLink(listing, out BazaarItemLink link, out failure))
                {
                    RemoveListingFromCache(bazaarItemId);
                    return false;
                }

                BazaarItems[bazaarItemId] = listing;
                BazaarItemLinks[bazaarItemId] = link;
                return true;
            }
        }

        public void RemoveListingFromCache(long bazaarItemId)
        {
            BazaarItems.TryRemove(bazaarItemId, out _);
            BazaarItemLinks.TryRemove(bazaarItemId, out _);
        }

        private static bool TryBuildLink(
            BazaarItemDTO listing,
            out BazaarItemLink link,
            out string failure)
        {
            link = null;
            failure = null;

            if (listing == null || listing.BazaarItemId <= 0 || listing.ItemInstanceId == Guid.Empty)
            {
                failure = "Listing identity is invalid";
                return false;
            }

            ItemInstanceDTO itemDto = DAOFactory.ItemInstanceDAO.LoadById(listing.ItemInstanceId);
            if (itemDto == null)
            {
                failure = $"ItemInstance {listing.ItemInstanceId} does not exist";
                return false;
            }

            if (itemDto.Type != Frostvein.Domain.InventoryType.Bazaar ||
                itemDto.CharacterId != listing.SellerId)
            {
                failure = $"ItemInstance owner/type mismatch: owner={itemDto.CharacterId}, type={itemDto.Type}";
                return false;
            }

            link = new BazaarItemLink
            {
                BazaarItem = listing,
                Item = new ItemInstance(itemDto),
                Owner = DAOFactory.CharacterDAO.LoadById(listing.SellerId)?.Name
            };
            return true;
        }
    }
}
