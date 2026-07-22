using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class FishingPositionDao : IFishingPositionDao
    {
        public SaveResult InsertOrUpdate(List<FishingPositionDto> fishes)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                foreach (var card in fishes)
                {
                    InsertOrUpdates(card);
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

        public IEnumerable<FishingPositionDto> LoadAll()
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<FishingPositionDto>();
            foreach (var entity in context.FishingPosition)
            {
                var dto = new FishingPositionDto();
                Mapper.Mappers.FishingPositionMapper.ToFishingPositionDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdates(FishingPositionDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.FishingPosition.FirstOrDefault(c => c.Id == CardId);

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

        private static FishingPositionDto insert(FishingPositionDto card, NosGmContext context)
        {
            var entity = new FishingPosition();
            Mapper.Mappers.FishingPositionMapper.ToFishingPosition(card, entity);
            context.FishingPosition.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.FishingPositionMapper.ToFishingPositionDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static FishingPositionDto update(FishingPosition entity, FishingPositionDto card, NosGmContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.FishingPositionMapper.ToFishingPosition(card, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.FishingPositionMapper.ToFishingPositionDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}
