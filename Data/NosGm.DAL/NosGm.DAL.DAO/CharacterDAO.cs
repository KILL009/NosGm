using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class CharacterDAO : ICharacterDAO
    {
        #region Methods

        public DeleteResult DeleteByPrimaryKey(long accountId, byte characterSlot)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    // actually a Character wont be deleted, it just will be disabled for future traces
                    var character = context.Character.SingleOrDefault(c =>
                        c.AccountId.Equals(accountId) && c.Slot.Equals(characterSlot) &&
                        c.State.Equals((byte)CharacterState.Active));

                    if (character != null)
                    {
                        character.State = (byte)CharacterState.Inactive;
                        character.Name = $"[DELETED]{character.Name}";
                        Logger.Info($"CharacterId {character.CharacterId} was deleted!");
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_CHARACTER_ERROR"), characterSlot, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public bool CheckNameAlreadyExists(string name)
        {
            var context = DataAccessHelper.CreateContext();
            return context.Account.Any(x => x.Name == name);
        }

        /// <summary>
        ///     Returns first 30 occurences of highest Compliment
        /// </summary>
        /// <returns></returns>
        public List<CharacterDTO> GetTopCompliment()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.State == (byte)CharacterState.Active && c.Account.Authority == AuthorityType.User &&
                                !c.Account.PenaltyLog.Any(l =>
                                    l.Penalty == PenaltyType.Banned && l.DateEnd > DateTime.Now))
                    .OrderByDescending(c => c.Compliment).Take(30))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        /// <summary>
        ///     Returns first 30 occurences of highest Act4Points
        /// </summary>
        /// <returns></returns>
        public List<CharacterDTO> GetTopPoints()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.State == (byte)CharacterState.Active && c.Account.Authority == AuthorityType.User &&
                                !c.Account.PenaltyLog.Any(l =>
                                    l.Penalty == PenaltyType.Banned && l.DateEnd > DateTime.Now))
                    .OrderByDescending(c => c.Act4Points).Take(30))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        /// <summary>
        ///     Returns first 30 occurences of highest Reputation
        /// </summary>
        /// <returns></returns>
        public List<CharacterDTO> GetTopReputation()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.State == (byte)CharacterState.Active && c.Account.Authority <= AuthorityType.GS &&
                                !c.Account.PenaltyLog.Any(l =>
                                    l.Penalty == PenaltyType.Banned && l.DateEnd > DateTime.Now))
                    .OrderByDescending(c => c.Reputation).Take(43))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        /// <summary>
        ///     Returns first 30 occurences of highest Duel
        /// </summary>
        /// <returns></returns>
        public List<CharacterDTO> GetTopDuel()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.State == (byte)CharacterState.Active && c.Account.Authority <= AuthorityType.GS &&
                                !c.Account.PenaltyLog.Any(l =>
                                    l.Penalty == PenaltyType.Banned && l.DateEnd > DateTime.Now))
                    .OrderByDescending(c => c.DuelWon).Take(43))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        /// <summary>
        ///     Returns first 30 occurences of highest Monster's killed
        /// </summary>
        /// <returns></returns>
        public List<CharacterDTO> GetTopMonster()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.State == (byte)CharacterState.Active && c.Account.Authority <= AuthorityType.GS &&
                                !c.Account.PenaltyLog.Any(l =>
                                    l.Penalty == PenaltyType.Banned && l.DateEnd > DateTime.Now))
                    .OrderByDescending(c => c.MonsterCount).Take(43))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public async Task<SaveResult> InsertOrUpdate(CharacterDTO character)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long characterId = character.CharacterId;
                    Character entity = await context.Character.FirstOrDefaultAsync(c => c.CharacterId.Equals(characterId)).ConfigureAwait(false);

                    if (entity == null)
                    {
                        character = await InsertAsync(character, context).ConfigureAwait(false);
                        if (character == null)
                        {
                            await LoggerService.LogServer.Logger.LogAsync($"Error updating character. Updated character is null. ID: {characterId}", LogType.ERROR);
                            return SaveResult.Error;
                        }
                        return SaveResult.Inserted;
                    }

                    character = await UpdateAsync(entity, character, context).ConfigureAwait(false);
                    if (character == null)
                    {
                        await LoggerService.LogServer.Logger.LogAsync($"Error updating character. Updated character is null. ID: {characterId}", LogType.ERROR);
                        return SaveResult.Error;
                    }
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), character, e.Message),
                    e);
                await LoggerService.LogServer.Logger.LogAsync($"Error inserting/updating character with ID {character.CharacterId}. Message: {e.Message}", LogType.ERROR);
                return SaveResult.Error;
            }
        }

        public IEnumerable<CharacterDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var chara in context.Character)
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(chara, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<CharacterDTO> LoadAllByAccount(long accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character.Where(c => c.AccountId.Equals(accountId))
                    .OrderByDescending(c => c.Slot))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<CharacterDTO> LoadByAccount(long accountId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CharacterDTO>();
                foreach (var entity in context.Character
                    .Where(c => c.AccountId.Equals(accountId) && c.State.Equals((byte)CharacterState.Active))
                    .OrderByDescending(c => c.Slot))
                {
                    var dto = new CharacterDTO();
                    CharacterMapper.ToCharacterDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public CharacterDTO LoadById(long characterId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CharacterDTO();
                    if (CharacterMapper.ToCharacterDTO(
                        context.Character.FirstOrDefault(c => c.CharacterId.Equals(characterId)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public CharacterDTO LoadByName(string name)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CharacterDTO();
                    if (CharacterMapper.ToCharacterDTO(context.Character.SingleOrDefault(c => c.Name.Equals(name)), dto)
                    ) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public CharacterDTO LoadBySlot(long accountId, byte slot)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new CharacterDTO();
                    if (CharacterMapper.ToCharacterDTO(
                        context.Character.SingleOrDefault(c =>
                            c.AccountId.Equals(accountId) && c.Slot.Equals(slot) &&
                            c.State.Equals((byte)CharacterState.Active)), dto)) return dto;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"There should be only 1 character per slot, AccountId: {accountId} Slot: {slot}", e);
                return null;
            }
        }

        private static CharacterDTO Insert(CharacterDTO character, NosGmContext context)
        {
            var entity = new Character();
            CharacterMapper.ToCharacter(character, entity);
            context.Character.Add(entity);
            context.SaveChanges();
            if (CharacterMapper.ToCharacterDTO(entity, character)) return character;
            return null;
        }

        private static CharacterDTO Update(Character entity, CharacterDTO character, NosGmContext context)
        {
            if (entity != null)
            {
                CharacterMapper.ToCharacter(character, entity);
                context.SaveChanges();
            }

            if (CharacterMapper.ToCharacterDTO(entity, character)) return character;

            return null;
        }

        public async Task<SaveResult> InsertOrUpdateAsync(CharacterDTO character)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long characterId = character.CharacterId;
                    Character entity = context.Character.FirstOrDefault(c => c.CharacterId.Equals(characterId));
                    if (entity == null)
                    {
                        character = await InsertAsync(character, context).ConfigureAwait(false);
                        return SaveResult.Inserted;
                    }
                    character = await UpdateAsync(entity, character, context).ConfigureAwait(false);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), character, e.Message), e);
                return SaveResult.Error;
            }
        }

        private async Task<CharacterDTO> UpdateAsync(Character entity, CharacterDTO character, NosGmContext context)
        {
            if (entity != null && character != null)
            {
                CharacterMapper.ToCharacter(character, entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (CharacterMapper.ToCharacterDTO(entity, character))
            {
                return character;
            }

            return null;
        }

        private async Task<CharacterDTO> InsertAsync(CharacterDTO character, NosGmContext context)
        {
            Character entity = new Character();
            CharacterMapper.ToCharacter(character, entity);
            context.Character.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            if (CharacterMapper.ToCharacterDTO(entity, character))
            {
                return character;
            }
            return null;
        }

        #endregion
    }
}