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
    public class RecipeDAO : IRecipeDAO
    {
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
                foreach (var Recipe in context.Recipe)
                {
                    var dto = new RecipeDTO();
                    RecipeMapper.ToRecipeDTO(Recipe, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RecipeDTO LoadById(short recipeId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RecipeDTO();
                    if (RecipeMapper.ToRecipeDTO(context.Recipe.SingleOrDefault(s => s.RecipeId.Equals(recipeId)), dto))
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