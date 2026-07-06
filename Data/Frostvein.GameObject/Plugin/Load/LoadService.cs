using Frostvein.Configuration;
using Frostvein.Domain;
using Frostvein.GameObject.Plugin.Event.Handler;
using Frostvein.LogServer.MongoDB;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class LoadService
    {
        public static void Load()
        {
             PluginLoadProperty.Load();
             PluginLoadBosses.Load();
             PluginLoadEvents.Load();
             PluginLoadItems.Load();
             PluginLoadMonsterDrop.Load();
             PluginLoadMonsterSkills.Load();
             PluginLoadBazaarItems.Load();
             PluginLoadNpcMonster.Load();
             PluginLoadRecipe.Load();
             PluginLoadRecipeList.Load();
             PluginLoadShopItem.Load();
             PluginLoadShopSkills.Load();
             PluginLoadShops.Load();
             PluginLoadTeleporter.Load();
             PluginLoadSkills.Load();
             PluginLoadCards.Load();
             PluginLoadQuest.Load();
             PluginLoadMapNpc.Load();
             PluginLoadMapsAndContent.Load();
             PluginLoadFamilies.Load();
             EventServiceHandler.LaunchEvents();
             PluginLoadCharacterRelations.Load();
            //Event
             RankingEvent.Load();
             MoritiusShipEvent.Load();
             GemStoneEvent.Load();
             BaseArenaEvent.Load();
             FamilyArenaEvent.Load();
             PluginLoadScriptedInstances.Load();
             PluginLoadBannedCharacters.Load();
             PluginLoadFishes.Load();
             PluginLoadTimespaceLogs.Load();
             PluginLoadBattlePass.Load();
             LogService.Generate(LogConfiguration.LoadOutput, LogType.LOAD);
             LogConfiguration.LoadOutput = string.Empty;
        }

        public static void Reload()
        {
            PluginLoadShopItem.Load();
            PluginLoadShopSkills.Load();
            PluginLoadShops.Load();
            PluginLoadTeleporter.Load();
            PluginLoadMapNpc.Load();
            PluginLoadMapsAndContent.Load();
        }
    }
}
