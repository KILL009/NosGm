using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class BCardDAO : IBCardDAO
    {
        #region Methods

        public DeleteResult DeleteByCardId(short cardId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    IEnumerable<BCard> bCards = context.BCard.Where(s => s.CardId == cardId);

                    foreach (var bcard in bCards) context.BCard.Remove(bcard);
                    context.SaveChanges();

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public DeleteResult DeleteByItemVNum(short itemVNum)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    IEnumerable<BCard> bCards = context.BCard.Where(s => s.ItemVNum == itemVNum);

                    foreach (var bcard in bCards) context.BCard.Remove(bcard);
                    context.SaveChanges();

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public DeleteResult DeleteByMonsterVNum(short monsterVNum)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    IEnumerable<BCard> bCards = context.BCard.Where(s => s.NpcMonsterVNum == monsterVNum);

                    foreach (var bcard in bCards) context.BCard.Remove(bcard);
                    context.SaveChanges();

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public DeleteResult DeleteBySkillVNum(short skillVNum)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    IEnumerable<BCard> bCards = context.BCard.Where(s => s.SkillVNum == skillVNum);

                    foreach (var bcard in bCards) context.BCard.Remove(bcard);
                    context.SaveChanges();

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public BCardDTO Insert(ref BCardDTO cardObject)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new BCard();
                    BCardMapper.ToBCard(cardObject, entity);
                    context.BCard.Add(entity);
                    context.SaveChanges();
                    if (BCardMapper.ToBCardDTO(entity, cardObject)) return cardObject;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public void Insert(List<BCardDTO> cards)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var card in cards)
                    {
                        var entity = new BCard();
                        BCardMapper.ToBCard(card, entity);
                        context.BCard.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public IEnumerable<BCardDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<BCardDTO>();
                foreach (var card in context.BCard)
                {
                    var dto = new BCardDTO();
                    BCardMapper.ToBCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<BCardDTO> LoadByCardId(short cardId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<BCardDTO>();
                foreach (var card in context.BCard.Where(s => s.CardId == cardId))
                {
                    var dto = new BCardDTO();
                    BCardMapper.ToBCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public BCardDTO LoadById(short cardId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new BCardDTO();
                    if (BCardMapper.ToBCardDTO(context.BCard.FirstOrDefault(s => s.BCardId.Equals(cardId)), dto))
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

        public IEnumerable<BCardDTO> LoadByItemVNum(short vNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<BCardDTO>();
                foreach (var card in context.BCard.Where(s => s.ItemVNum == vNum))
                {
                    var dto = new BCardDTO();
                    BCardMapper.ToBCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<BCardDTO> LoadByNpcMonsterVNum(short vNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<BCardDTO>();
                foreach (var card in context.BCard.Where(s => s.NpcMonsterVNum == vNum))
                {
                    var dto = new BCardDTO();
                    BCardMapper.ToBCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<BCardDTO> LoadBySkillVNum(short vNum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<BCardDTO>();
                foreach (var card in context.BCard.Where(s => s.SkillVNum == vNum))
                {
                    var dto = new BCardDTO();
                    BCardMapper.ToBCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}