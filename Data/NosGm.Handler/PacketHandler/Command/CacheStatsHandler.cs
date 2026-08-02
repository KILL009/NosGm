using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.DAL;

namespace NosGm.Handler.PacketHandler.Command
{
    public class CacheStatsHandler : IPacketHandler
    {
        public CacheStatsHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void Command(CacheStatsPacket cacheStatsPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("[CACHE] ----------- CACHE METRICS -----------", 11));

            var itemStats = DAOFactory.ItemDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] Item Stored={itemStats.StoredItems} Hits={itemStats.CacheHits} Misses={itemStats.CacheMisses} Reloads={itemStats.FullReloads}", 11));

            var mapStats = DAOFactory.MapDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] Map Stored={mapStats.StoredItems} Hits={mapStats.CacheHits} Misses={mapStats.CacheMisses} Reloads={mapStats.FullReloads}", 11));

            var npcStats = DAOFactory.NpcMonsterDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] NpcMonster Stored={npcStats.StoredItems} Hits={npcStats.CacheHits} Misses={npcStats.CacheMisses} Reloads={npcStats.FullReloads}", 11));

            var skillStats = DAOFactory.SkillDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] Skill Stored={skillStats.StoredItems} Hits={skillStats.CacheHits} Misses={skillStats.CacheMisses} Reloads={skillStats.FullReloads}", 11));

            var cardStats = DAOFactory.CardDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] Card Stored={cardStats.StoredItems} Hits={cardStats.CacheHits} Misses={cardStats.CacheMisses} Reloads={cardStats.FullReloads}", 11));

            var recipeStats = DAOFactory.RecipeDAO.GetCacheStatistics();
            Session.SendPacket(Session.Character.GenerateSay($"[CACHE] Recipe Stored={recipeStats.StoredItems} Hits={recipeStats.CacheHits} Misses={recipeStats.CacheMisses} Reloads={recipeStats.FullReloads}", 11));
            
            Session.SendPacket(Session.Character.GenerateSay("[CACHE] -------------------------------------", 11));
        }
    }
}
