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
    public class BattlePassQuestDAO : IBattlePassQuestDAO
    {
        public SaveResult InsertOrUpdate(ref BattlePassQuestDTO battlePassQuest)
        {
            try
            {
                using (FrostveinContext context = DataAccessHelper.CreateContext())
                {
                    long id = battlePassQuest.BpQuestId;
                    BattlePassQuest entity = context.BattlePassQuest.FirstOrDefault(c => c.BpQuestId.Equals(id));

                    if (entity == null)
                    {
                        battlePassQuest = Insert(battlePassQuest, context);
                        return SaveResult.Inserted;
                    }
                    battlePassQuest = Update(entity, battlePassQuest, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("UPDATE_ACCOUNT_ERROR"), battlePassQuest.BpQuestId, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<BattlePassQuestDTO> LoadAll()
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassQuestDTO> result = new List<BattlePassQuestDTO>();
                foreach (BattlePassQuest prize in context.BattlePassQuest)
                {
                    BattlePassQuestDTO dto = new BattlePassQuestDTO();
                    Mapper.Mappers.BattlePassQuestMapper.ToBpQuestDTO(prize, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        private BattlePassQuestDTO Update(BattlePassQuest entity, BattlePassQuestDTO battlePass, FrostveinContext connection)
        {
            if (entity != null)
            {
                Mapper.Mappers.BattlePassQuestMapper.ToBpQuest(battlePass, entity);
                connection.SaveChanges();
            }
            if (Mapper.Mappers.BattlePassQuestMapper.ToBpQuestDTO(entity, battlePass))
            {
                return battlePass;
            }

            return null;
        }

        private BattlePassQuestDTO Insert(BattlePassQuestDTO battlePass, FrostveinContext connection)
        {
            BattlePassQuest entity = new BattlePassQuest();
            Mapper.Mappers.BattlePassQuestMapper.ToBpQuest(battlePass, entity);
            connection.BattlePassQuest.Add(entity);
            connection.SaveChanges();
            Mapper.Mappers.BattlePassQuestMapper.ToBpQuestDTO(entity, battlePass);
            return battlePass;
        }

        public void Insert(List<BattlePassQuestDTO> battlePassQuests)
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassQuest> battlePasses = new List<BattlePassQuest>();

                foreach (var battlePass in battlePassQuests)
                {
                    BattlePassQuest passQuest = new BattlePassQuest();
                    Mapper.Mappers.BattlePassQuestMapper.ToBpQuest(battlePass, passQuest);
                    battlePasses.Add(passQuest);
                }

                context.BattlePassQuest.AddRange(battlePasses);
                context.SaveChanges();
            }
        }
    }
}