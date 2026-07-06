using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.DAL.DAO
{
    public class CharacterFishDao : ICharacterFishDao
    {
        public IEnumerable<CharacterFishDto> LoadByCharacterId(long characterId)
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<CharacterFishDto>();
            foreach (var entity in context.CharacterFish.Where(s => s.CharacterId == characterId))
            {
                var dto = new CharacterFishDto();
                Mapper.Mappers.CharacterFIshMapper.ToCharacterFishDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdateFromList(IEnumerable<CharacterFishDto> fishes)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();

                foreach (var card in fishes)
                {
                    InsertOrUpdate(fishes);
                }
                context.SaveChanges();
                return SaveResult.Inserted;
            }
            catch (Exception e)
            {
                Logger.Log.Error("InsertOrUpdateFromList", e);
                return SaveResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(IEnumerable<CharacterFishDto> fishes)
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

        public SaveResult InsertOrUpdates(CharacterFishDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.CharacterFish.FirstOrDefault(c => c.Id == CardId);

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

        private static CharacterFishDto insert(CharacterFishDto card, FrostveinContext context)
        {
            var entity = new CharacterFish();
            Mapper.Mappers.CharacterFIshMapper.ToCharacterFish(card, entity);
            context.CharacterFish.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.CharacterFIshMapper.ToCharacterFishDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static CharacterFishDto update(CharacterFish entity, CharacterFishDto card, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.CharacterFIshMapper.ToCharacterFish(card, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.CharacterFIshMapper.ToCharacterFishDto(entity, card))
            {
                return card;
            }

            return null;
        }

        public async Task<SaveResult> InsertOrUpdateAsync(IEnumerable<CharacterFishDto> fishes)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var card in fishes)
                    {
                        InsertOrUpdates(card);
                    }
                    context.Configuration.AutoDetectChangesEnabled = true;
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    return SaveResult.Inserted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public async Task<SaveResult> InsertOrUpdatesAsync(CharacterFishDto card)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long CardId = card.Id;
                    var entity = context.CharacterFish.FirstOrDefault(c => c.Id == CardId);

                    if (entity == null)
                    {
                        card = await InsertAsync(card, context).ConfigureAwait(false);
                        return SaveResult.Inserted;
                    }

                    card = await UpdateAsync(entity, card, context).ConfigureAwait(false);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_CARD_ERROR"), card.Id, e.Message), e);
                return SaveResult.Error;
            }
        }

        private static async Task<CharacterFishDto> InsertAsync(CharacterFishDto card, FrostveinContext context)
        {
            var entity = new CharacterFish();
            Mapper.Mappers.CharacterFIshMapper.ToCharacterFish(card, entity);
            context.CharacterFish.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            if (Mapper.Mappers.CharacterFIshMapper.ToCharacterFishDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static async Task<CharacterFishDto> UpdateAsync(CharacterFish entity, CharacterFishDto card, FrostveinContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.CharacterFIshMapper.ToCharacterFish(card, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (Mapper.Mappers.CharacterFIshMapper.ToCharacterFishDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}
