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
    public class RecipeDAO : IRecipeDAO
    {
        private static readonly ICacheService<short, RecipeDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, RecipeDTO>();

        #region Methods

        public RecipeDTO Insert(RecipeDTO recipe)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Recipe();
                    RecipeMapper.ToRecipe(recipe, entity);
                    context.Recipe.Add(entity);
                    context.SaveChanges();
                    if (RecipeMapper.ToRecipeDTO(entity, recipe)) return recipe;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RecipeDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RecipeDTO>();
                foreach (var Recipe in context.Recipe.AsNoTracking())
                {
                    var dto = new RecipeDTO();
                    RecipeMapper.ToRecipeDTO(Recipe, dto);
                    _cache.Set(dto.RecipeId, dto, TimeSpan.FromHours(24));
                    result.Add(dto);
                }

                return result;
            }
        }

        public RecipeDTO LoadById(short recipeId)
        {
            try
            {
                if (_cache.TryGetValue(recipeId, out var cachedDto))
                {
                    return cachedDto;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RecipeDTO();
                    if (RecipeMapper.ToRecipeDTO(context.Recipe.AsNoTracking().SingleOrDefault(s => s.RecipeId.Equals(recipeId)), dto))
                    {
                        _cache.Set(recipeId, dto, TimeSpan.FromHours(24));
                        return dto;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public RecipeDTO LoadByItemVNum(short itemVNum)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RecipeDTO();
                    if (RecipeMapper.ToRecipeDTO(context.Recipe.SingleOrDefault(s => s.ItemVNum.Equals(itemVNum)), dto))
                        return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Update(RecipeDTO recipe)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = context.Recipe.FirstOrDefault(c => c.ItemVNum == recipe.ItemVNum);
                    if (result != null)
                    {
                        recipe.RecipeId = result.RecipeId;
                        RecipeMapper.ToRecipe(recipe, result);
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