using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportRecipe : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportRecipe(ImportConfiguration configuration) => _configuration = configuration;

        public void Import()
        {
            var count = 0;
            var mapNpcId = 0;
            short itemVNum = 0;
            RecipeDTO recipe;
            RecipeListDTO recipeListDTO;

            foreach (var currentPacket in _configuration.Packets.Where(o =>
                o[0].Equals("n_run") || o[0].Equals("pdtse") || o[0].Equals("m_list")))
            {
                if (currentPacket.Length > 4 && currentPacket[0] == "n_run")
                {
                    int.TryParse(currentPacket[4], out mapNpcId);
                    continue;
                }

                if (currentPacket.Length > 1 && currentPacket[0] == "m_list" &&
                    (currentPacket[1] == "2" || currentPacket[1] == "4"))
                {
                    for (var i = 2; i < currentPacket.Length - 1; i++)
                    {
                        var vNum = short.Parse(currentPacket[i]);
                        if (DAOFactory.RecipeDAO.LoadByItemVNum(vNum) == null)
                        {
                            recipe = new RecipeDTO
                            {
                                ItemVNum = vNum
                            };
                            DAOFactory.RecipeDAO.Insert(recipe);
                        }

                        var recipeForId = DAOFactory.RecipeDAO.LoadByItemVNum(vNum);
                        if (recipeForId == null)
                        {
                            continue;
                        }

                        if (DAOFactory.MapNpcDAO.LoadById(mapNpcId) != null && !DAOFactory.RecipeListDAO
                                                                                          .LoadByMapNpcId(mapNpcId).Any(r => r.RecipeId.Equals(recipeForId.RecipeId)))
                        {
                            recipeListDTO = new RecipeListDTO
                            {
                                MapNpcId = mapNpcId,
                                RecipeId = recipeForId.RecipeId
                            };

                            DAOFactory.RecipeListDAO.Insert(recipeListDTO);
                            count++;
                        }
                    }

                    continue;
                }

                if (currentPacket.Length > 2 && currentPacket[0] == "pdtse")
                {
                    itemVNum = short.Parse(currentPacket[2]);
                    continue;
                }

                if (currentPacket.Length <= 1 || currentPacket[0] != "m_list" ||
                    currentPacket[1] != "3" && currentPacket[1] != "5")
                {
                    continue;
                }

                for (var i = 3; i < currentPacket.Length - 1; i += 2)
                {
                    var rec = DAOFactory.RecipeDAO.LoadByItemVNum(itemVNum);
                    if (rec != null)
                    {
                        rec.Amount = short.Parse(currentPacket[2]);
                        DAOFactory.RecipeDAO.Update(rec);
                        var recipeitem = new RecipeItemDTO
                        {
                            ItemVNum = short.Parse(currentPacket[i]),
                            Amount = short.Parse(currentPacket[i + 1]),
                            RecipeId = rec.RecipeId
                        };
                        if (!DAOFactory.RecipeItemDAO.LoadByRecipeAndItem(rec.RecipeId, recipeitem.ItemVNum).Any())
                        {
                            DAOFactory.RecipeItemDAO.Insert(recipeitem);
                        }
                    }
                }

                itemVNum = -1;
            }

            Logger.Log.Info($"{count} Recipes parsed");
        }
    }
}