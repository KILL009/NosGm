using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class FishingInformationsDao : IFishingInformationsDao
    {
        public SaveResult InsertorUpdate(List<FishingInformationsDto> fishes)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                foreach (var card in fishes)
                {
                    InsertOrUpdate(card);
                }
                context.Configuration.AutoDetectChangesEnabled = true;
                context.SaveChanges();
                return SaveResult.Inserted;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<FishingInformationsDto> LoadAll()
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<FishingInformationsDto>();
            foreach (var entity in context.FishingInformations)
            {
                var dto = new FishingInformationsDto();
                Mapper.Mappers.FishingInformationsMapper.ToFishingInformationsDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdate(FishingInformationsDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.FishingInformations.FirstOrDefault(c => c.Id == CardId);

                if (entity == null)
                {
                    card = insert(card, context);
                    return SaveResult.Inserted;
                }

                card = update(entity, card, context);
                return SaveResult.Updated;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_CARD_ERROR"), card.Id, e.Message), e);
                return SaveResult.Error;
            }
        }

        private static FishingInformationsDto insert(FishingInformationsDto card, FrostveinContext context)
        {
            var entity = new FishingInformations();
            Mapper.Mappers.FishingInformationsMapper.ToFishingInformations(card, entity);
            context.FishingInformations.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.FishingInformationsMapper.ToFishingInformationsDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static FishingInformationsDto update(FishingInformations entity, FishingInformationsDto card, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.FishingInformationsMapper.ToFishingInformations(card, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.FishingInformationsMapper.ToFishingInformationsDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}
