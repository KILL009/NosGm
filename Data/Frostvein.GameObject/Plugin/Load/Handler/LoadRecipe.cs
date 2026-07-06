using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadRecipe
    {
        public static void Load()
        {
            var recipes = DAOFactory.RecipeDAO.LoadAll();
            var recipeItems = DAOFactory.RecipeItemDAO.LoadAll();
            IEnumerable<RecipeItemDTO> recipeItemDtos = recipeItems.ToList();

            ServerManager.Instance._recipes = new ThreadSafeSortedList<short, Recipe>();

            foreach (var recipeGrouping in recipes)
            {
                var recipe = new Recipe(recipeGrouping)
                {
                    Items = new List<RecipeItemDTO>()
                };
                recipe.Items.AddRange(recipeItemDtos.Where(s => s.RecipeId == recipe.RecipeId));
                ServerManager.Instance._recipes[recipeGrouping.RecipeId] = recipe;
            }
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._recipes.Count} Recipes - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
