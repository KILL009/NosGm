using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NosGm.DAL.DAO
{
    public class CardDAO : ICardDAO
    {
        private static readonly ICacheService<short, CardDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, CardDTO>(dto => dto.Clone());
        private static int _isFullyLoaded;
        private static readonly object _loadLock = new object();

        #region Methods

        public CardDTO Insert(ref CardDTO card)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Card();
                    CardMapper.ToCard(card, entity);
                    context.Card.Add(entity);
                    context.SaveChanges();
                    if (CardMapper.ToCardDTO(entity, card))
                    {
                        _cache.Set(card.CardId, card);
                        return card;
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

        public void Insert(List<CardDTO> cards)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var card in cards) InsertOrUpdate(card);
                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }

                _cache.Clear();
                Volatile.Write(ref _isFullyLoaded, 0);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public SaveResult InsertOrUpdate(CardDTO card)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long CardId = card.CardId;
                    var entity = context.Card.FirstOrDefault(c => c.CardId == CardId);

                    if (entity == null)
                    {
                        card = insert(card, context);
                        if (card != null) _cache.Set(card.CardId, card);
                        return SaveResult.Inserted;
                    }

                    card = update(entity, card, context);
                    if (card != null) _cache.Set(card.CardId, card);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_CARD_ERROR"), card.CardId, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<CardDTO> LoadAll()
        {
            if (Volatile.Read(ref _isFullyLoaded) == 1)
            {
                return _cache.GetAll();
            }

            lock (_loadLock)
            {
                if (Volatile.Read(ref _isFullyLoaded) == 1)
                {
                    return _cache.GetAll();
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = new List<CardDTO>();
                    var cacheItems = new List<KeyValuePair<short, CardDTO>>();
                    foreach (var card in context.Card.AsNoTracking())
                    {
                        var dto = new CardDTO();
                        CardMapper.ToCardDTO(card, dto);
                        cacheItems.Add(new KeyValuePair<short, CardDTO>(dto.CardId, dto));
                        result.Add(dto);
                    }

                    _cache.ReplaceAll(cacheItems);
                    Volatile.Write(ref _isFullyLoaded, 1);
                    return result;
                }
            }
        }

        public CardDTO LoadById(short cardId)
        {
            try
            {
                if (_cache.TryGetValue(cardId, out var cachedDto))
                {
                    return cachedDto;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CardDTO();
                    if (CardMapper.ToCardDTO(context.Card.AsNoTracking().FirstOrDefault(s => s.CardId.Equals(cardId)), dto))
                    {
                        _cache.Set(cardId, dto);
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

        private static CardDTO insert(CardDTO card, NosGmContext context)
        {
            var entity = new Card();
            CardMapper.ToCard(card, entity);
            context.Card.Add(entity);
            context.SaveChanges();
            if (CardMapper.ToCardDTO(entity, card)) return card;

            return null;
        }

        private static CardDTO update(Card entity, CardDTO card, NosGmContext context)
        {
            if (entity != null)
            {
                CardMapper.ToCard(card, entity);
                context.SaveChanges();
            }

            if (CardMapper.ToCardDTO(entity, card)) return card;

            return null;
        }

        #endregion
    }
}