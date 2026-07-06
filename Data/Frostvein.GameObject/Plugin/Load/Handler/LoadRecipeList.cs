using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadRecipeList
    {
        public static void Load()
        {
            ServerManager.Instance._recipeLists = new ThreadSafeSortedList<int, RecipeListDTO>();
            foreach (var recipeListGrouping in DAOFactory.RecipeListDAO.LoadAll())
            {
                ServerManager.Instance._recipeLists[recipeListGrouping.RecipeListId] = recipeListGrouping;
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._recipeLists.Count} Recipe Lists - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
