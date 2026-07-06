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
    public class CharacterTimespaceLogDAO : ICharacterTimespaceLogDAO
    {
        public CharacterTimespaceLogDTO GetHighestScoreByScriptedInstanceId(long scriptedInstanceId)
        {
            var context = DataAccessHelper.CreateContext();
            var entity = context.CharacterTimespaceLog.Where(s => s.ScriptedInstanceId == scriptedInstanceId).OrderByDescending(s => s.Score).FirstOrDefault();
            var dto = new CharacterTimespaceLogDTO();
            CharacterTimespaceLogMapper.ToTimespaceLogDTO(entity, dto);
            return dto;
        }

        public List<CharacterTimespaceLogDTO> LoadAll()
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<CharacterTimespaceLogDTO>();
            foreach (var battlePassItem in context.CharacterTimespaceLog)
            {
                var dto = new CharacterTimespaceLogDTO();
                CharacterTimespaceLogMapper.ToTimespaceLogDTO(battlePassItem, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdateFromList(IEnumerable<CharacterTimespaceLogDTO> logs)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                foreach (var card in logs)
                {
                    InsertOrUpdate(card);
                }
                context.Configuration.AutoDetectChangesEnabled = true;
                context.SaveChanges();
                return SaveResult.Inserted;
            }
            catch (Exception e)
            {
                Logger.Log.Error("InsertOrUpdateFromList", e);
                return SaveResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(CharacterTimespaceLogDTO card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long logId = card.ScriptedInstanceId;
                var entity = context.CharacterTimespaceLog.FirstOrDefault(c => c.Date == card.Date && c.CharacterId == card.CharacterId && c.Score == card.Score);

                if (entity == null)
                {
                    card = insert(card, context);
                    return SaveResult.Inserted;
                }

                var r = new CharacterTimespaceLog();
                CharacterTimespaceLogMapper.ToTimespaceLog(card, r);
                var z = new CharacterTimespaceLogDTO();
                CharacterTimespaceLogMapper.ToTimespaceLogDTO(entity, z);
                update(r, z, context);
                return SaveResult.Updated;
            }
            catch (Exception e)
            {
                Logger.Log.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_CARD_ERROR"), card.LogId, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<CharacterTimespaceLogDTO> LoadByCharactedId(long characterId)
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<CharacterTimespaceLogDTO>();
            foreach (var entity in context.CharacterTimespaceLog.Where(s => s.CharacterId == characterId))
            {
                var dto = new CharacterTimespaceLogDTO();
                CharacterTimespaceLogMapper.ToTimespaceLogDTO(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public bool IdAlreadySet(long id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.CharacterTimespaceLog.Any(gl => gl.LogId == id);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public CharacterTimespaceLogDTO Insert(CharacterTimespaceLogDTO timespaceLog)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new CharacterTimespaceLog();
                    CharacterTimespaceLogMapper.ToTimespaceLog(timespaceLog, entity);
                    context.CharacterTimespaceLog.Add(entity);
                    context.SaveChanges();
                    if (CharacterTimespaceLogMapper.ToTimespaceLogDTO(entity, timespaceLog)) return timespaceLog;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static CharacterTimespaceLogDTO insert(CharacterTimespaceLogDTO card, FrostveinContext context)
        {
            var entity = new CharacterTimespaceLog();
            CharacterTimespaceLogMapper.ToTimespaceLog(card, entity);
            context.CharacterTimespaceLog.Add(entity);
            context.SaveChanges();
            if (CharacterTimespaceLogMapper.ToTimespaceLogDTO(entity, card))
            {
                return card;
            }

            return null;
        }

        
        private static void update(CharacterTimespaceLog entity, CharacterTimespaceLogDTO card, FrostveinContext context)
        {
            context.CharacterTimespaceLog.AddOrUpdateExtension(entity);
            context.SaveChanges();
        }
    }
}
