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

namespace NosGm.DAL.DAO
{
    public class CardDAO : ICardDAO
    {
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
                    if (CardMapper.ToCardDTO(entity, card)) return card;

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
                        return SaveResult.Inserted;
                    }

                    card = update(entity, card, context);
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
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CardDTO>();
                foreach (var card in context.Card)
                {
                    var dto = new CardDTO();
                    CardMapper.ToCardDTO(card, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public CardDTO LoadById(short cardId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CardDTO();
                    if (CardMapper.ToCardDTO(context.Card.FirstOrDefault(s => s.CardId.Equals(cardId)), dto))
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