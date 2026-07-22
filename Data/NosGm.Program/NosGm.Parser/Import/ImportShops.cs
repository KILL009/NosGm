using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportShops : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportShops(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Import()
        {
            var shopCounter = 0;

            var shops = new List<ShopDTO>();

            var npcs = DAOFactory.MapNpcDAO.LoadAll().ToDictionary(s => s.MapNpcId, s => s);
            var existingShopOwners = DAOFactory.ShopDAO.LoadAll().Select(x => x.MapNpcId).ToHashSet();

            foreach (var currentPacket in _configuration.Packets.Where(o =>
                o.Length > 6 && o[0].Equals("shop") && o[1].Equals("2")))
            {
                if (!npcs.TryGetValue(short.Parse(currentPacket[2]), out var npc)) continue;

                var name = string.Empty;
                for (var j = 6; j < currentPacket.Length; j++) name += $"{currentPacket[j]} ";

                name = name.Trim();

                var shop = new ShopDTO
                {
                    Name = name,
                    MapNpcId = npc.MapNpcId,
                    MenuType = byte.Parse(currentPacket[4]),
                    ShopType = byte.Parse(currentPacket[5])
                };

                if (existingShopOwners.Contains(npc.MapNpcId) || shops.Any(s => s.MapNpcId == npc.MapNpcId)) continue;

                shops.Add(shop);
                shopCounter++;
            }

            DAOFactory.ShopDAO.Insert(shops);
            Logger.Log.Info($"{shopCounter} Shops parsed");
        }
    }
}