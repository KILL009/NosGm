using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class RecipeListDAO : IRecipeListDAO
    {
        #region Methods

        public RecipeListDTO Insert(RecipeListDTO recipeList)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new RecipeList();
                    RecipeListMapper.ToRecipeList(recipeList, entity);
                    context.RecipeList.Add(entity);
                    context.SaveChanges();
                    if (RecipeListMapper.ToRecipeListDTO(entity, recipeList)) return recipeList;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RecipeListDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeListDTO>();
                foreach (var recipeList in context.RecipeList)
                {
                    var dto = new RecipeListDTO();
                    RecipeListMapper.ToRecipeListDTO(recipeList, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RecipeListDTO LoadById(int recipeListId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RecipeListDTO();
                    if (RecipeListMapper.ToRecipeListDTO(
                        context.RecipeList.SingleOrDefault(s => s.RecipeListId.Equals(recipeListId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RecipeListDTO> LoadByItemVNum(short itemVNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeListDTO>();
                foreach (var recipeList in context.RecipeList.Where(r => r.ItemVNum == itemVNum))
                {
                    var dto = new RecipeListDTO();
                    RecipeListMapper.ToRecipeListDTO(recipeList, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<RecipeListDTO> LoadByMapNpcId(int mapNpcId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeListDTO>();
                foreach (var recipeList in context.RecipeList.Where(r => r.MapNpcId == mapNpcId))
                {
                    var dto = new RecipeListDTO();
                    RecipeListMapper.ToRecipeListDTO(recipeList, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<RecipeListDTO> LoadByRecipeId(short recipeId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeListDTO>();
                foreach (var recipeList in context.RecipeList.Where(r => r.RecipeId.Equals(recipeId)))
                {
                    var dto = new RecipeListDTO();
                    RecipeListMapper.ToRecipeListDTO(recipeList, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public void Update(RecipeListDTO recipe)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = context.RecipeList.FirstOrDefault(r => r.RecipeListId.Equals(recipe.RecipeListId));
                    if (result != null)
                    {
                        RecipeListMapper.ToRecipeList(recipe, result);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #endregion
    }
}