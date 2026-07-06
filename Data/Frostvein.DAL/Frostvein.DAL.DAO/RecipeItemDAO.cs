using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class RecipeItemDAO : IRecipeItemDAO
    {
        #region Methods

        public RecipeItemDTO Insert(RecipeItemDTO recipeItem)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new RecipeItem();
                    RecipeItemMapper.ToRecipeItem(recipeItem, entity);
                    context.RecipeItem.Add(entity);
                    context.SaveChanges();
                    if (RecipeItemMapper.ToRecipeItemDTO(entity, recipeItem)) return recipeItem;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RecipeItemDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeItemDTO>();
                foreach (var recipeItem in context.RecipeItem)
                {
                    var dto = new RecipeItemDTO();
                    RecipeItemMapper.ToRecipeItemDTO(recipeItem, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RecipeItemDTO LoadById(short recipeItemId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RecipeItemDTO();
                    if (RecipeItemMapper.ToRecipeItemDTO(
                        context.RecipeItem.FirstOrDefault(s => s.RecipeItemId.Equals(recipeItemId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RecipeItemDTO> LoadByRecipe(short recipeId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeItemDTO>();
                foreach (var recipeItem in context.RecipeItem.Where(s => s.RecipeId.Equals(recipeId)))
                {
                    var dto = new RecipeItemDTO();
                    RecipeItemMapper.ToRecipeItemDTO(recipeItem, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<RecipeItemDTO> LoadByRecipeAndItem(short recipeId, short itemVNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeItemDTO>();
                foreach (var recipeItem in context.RecipeItem.Where(s =>
                    s.ItemVNum.Equals(itemVNum) && s.RecipeId.Equals(recipeId)))
                {
                    var dto = new RecipeItemDTO();
                    RecipeItemMapper.ToRecipeItemDTO(recipeItem, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}