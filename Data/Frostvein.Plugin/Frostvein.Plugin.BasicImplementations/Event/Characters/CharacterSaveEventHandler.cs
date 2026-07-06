using ChickenAPI.Events;


using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Plugins.BasicImplementations.Event.Characters
{
    public class CharacterSaveEventHandler : GenericEventHandlerBase<CharacterSaveEvent> 
    {

        protected override async void Handle(CharacterSaveEvent e, CancellationToken cancellation)
        {
            Character character = e.Sender.Character;

            await HandleSaveAsync(character);
        }

        private async Task HandleSaveAsync(Character character)
        {
            try
            {
                #region Character Save

                CharacterDTO characterDTO = character.DeepCopy();

                await DAOFactory.CharacterDAO.InsertOrUpdateAsync(characterDTO);

                #endregion

                #region Inventory Save
                if (character.Inventory != null)
                {
                    // load and concat inventory with equipment
                    List<ItemInstance> inventories = character.Inventory.GetAllItems();
                    IEnumerable<Guid> currentlySavedInventoryIds = DAOFactory.ItemInstanceDAO.LoadSlotAndTypeByCharacterId(character.CharacterId);
                    IEnumerable<CharacterDTO> characters = DAOFactory.CharacterDAO.LoadAllByAccount(character.Session.Account.AccountId);
                    foreach (CharacterDTO characteraccount in characters.Where(s => s.CharacterId != character.CharacterId))
                    {
                        currentlySavedInventoryIds = currentlySavedInventoryIds.Concat(DAOFactory.ItemInstanceDAO.LoadByCharacterId(characteraccount.CharacterId).Where(s => s.Type == InventoryType.Warehouse).Select(i => i.Id).ToList());
                    }

                    IEnumerable<MinilandObjectDTO> currentlySavedMinilandObjectEntries = DAOFactory.MinilandObjectDAO.LoadByCharacterId(character.CharacterId).ToList();
                    foreach (MinilandObjectDTO mobjToDelete in currentlySavedMinilandObjectEntries.Except(
                        character.MinilandObjects))
                    {
                        try
                        {
                            DAOFactory.MinilandObjectDAO.DeleteById(mobjToDelete.MinilandObjectId);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                        }
                    }

                    DAOFactory.ItemInstanceDAO.DeleteGuidList(currentlySavedInventoryIds.Except(inventories.Select(i => i.Id)));

                    // create or update all which are new or do still exist
                    List<ItemInstance> saveInventory = inventories.Where(s => s.Type != InventoryType.Bazaar && s.Type != InventoryType.FamilyWareHouse).ToList();

                    await DAOFactory.ItemInstanceDAO.InsertOrUpdateFromListAsync(saveInventory);

                    foreach (ItemInstance itemInstance in saveInventory)
                    {
                        await DAOFactory.ShellEffectDAO.InsertOrUpdateFromListAsync(itemInstance.ShellEffects, itemInstance.EquipmentSerialId);
                        await DAOFactory.CellonOptionDAO.InsertOrUpdateFromListAsync(itemInstance.CellonOptions, itemInstance.EquipmentSerialId);
                        DAOFactory.RuneEffectDAO.InsertOrUpdateFromList(itemInstance.RuneEffects, itemInstance.EquipmentSerialId);
                        DAOFactory.FairyEnchantmentDAO.InsertOrUpdateFromList(itemInstance.FairyEnchantments, itemInstance.EquipmentSerialId);

                    }
                }
                #endregion 

                #region Skill Save
                if (character.Skills != null)
                {
                    try
                    {
                        IEnumerable<Guid> currentlySavedCharacterSkills = DAOFactory.CharacterSkillDAO.LoadKeysByCharacterId(character.CharacterId).ToList();

                        foreach (Guid characterSkillToDeleteId in currentlySavedCharacterSkills.Except(character.Skills.Select(s => s.Id)))
                        {
                            DAOFactory.CharacterSkillDAO.Delete(characterSkillToDeleteId);
                        }

                        foreach (CharacterSkill characterSkill in character.Skills.GetAllItems())
                        {
                            await DAOFactory.CharacterSkillDAO.InsertOrUpdateAsync(characterSkill);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                    }
                }
                #endregion

                #region Title Save
                if (character.Title != null)
                {
                    foreach (var tit in character.Title)
                    {
                        await DAOFactory.CharacterTitleDAO.InsertOrUpdateAsync(tit);
                    }
                }
                #endregion

                #region Mate Save
                IEnumerable<long> currentlySavedMates = DAOFactory.MateDAO.LoadByCharacterId(character.CharacterId).Select(s => s.MateId);

                foreach (long mateToDeleteId in currentlySavedMates.Except(character.Mates.Select(s => s.MateId)))
                {
                    try
                    {
                        DAOFactory.MateDAO.Delete(mateToDeleteId);
                    }
                    catch (Exception e)
                    {
                        Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", e);
                    }
                }

                foreach (Mate mate in character.Mates)
                {
                    await DAOFactory.MateDAO.InsertOrUpdateAsync(mate);
                }
                #endregion

                #region Quicklist Save
                IEnumerable<QuicklistEntryDTO> quickListEntriesToInsertOrUpdateAsync = character.QuicklistEntries.ToList();

                try
                {
                    IEnumerable<Guid> currentlySavedQuicklistEntries = DAOFactory.QuicklistEntryDAO.LoadKeysByCharacterId(character.CharacterId).ToList();
                    foreach (Guid quicklistEntryToDelete in currentlySavedQuicklistEntries.Except(character.QuicklistEntries.Select(s => s.Id)))
                    {
                        DAOFactory.QuicklistEntryDAO.Delete(quicklistEntryToDelete);
                    }

                    foreach (QuicklistEntryDTO quicklistEntry in quickListEntriesToInsertOrUpdateAsync)
                    {
                        await DAOFactory.QuicklistEntryDAO.InsertOrUpdateAsync(quicklistEntry);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                }

                #endregion

                #region Miniland Save
                if (character.MinilandObjects.Count > 0)
                {
                    lock (character.MinilandObjects)
                    {
                        foreach (MinilandObjectDTO minilandObject in character.MinilandObjects)
                        {
                            try
                            {
                                DAOFactory.MinilandObjectDAO.InsertOrUpdateAsync(minilandObject);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                            }
                        }
                    }
                }

                #endregion

                #region StaticBuff Save
                IEnumerable<short> currentlySavedBuff = DAOFactory.StaticBuffDAO.LoadByTypeCharacterId(character.CharacterId).ToList();

                if (currentlySavedBuff.Count() > 0)
                {
                    foreach (short bonusToDelete in currentlySavedBuff.Except(character.Buff.Select(s => s.Card.CardId)))
                    {
                        try
                        {
                            DAOFactory.StaticBuffDAO.Delete(bonusToDelete, character.CharacterId);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                        }
                    }
                }

                if (character._isStaticBuffListInitial)
                {
                    foreach (Buff buff in character.Buff.Where(s => s.StaticBuff).ToArray())
                    {
                        try
                        {
                            if (buff.Card.CardId == 360 || buff.Card.CardId == 361) //GLOBAL FAMILY BUFFS
                                continue;

                            StaticBuffDTO bf = new StaticBuffDTO
                            {
                                CharacterId = character.CharacterId,
                                RemainingTime = (int)(buff.RemainingTime - (DateTime.Now - buff.Start).TotalSeconds),
                                CardId = buff.Card.CardId
                            };
                            await DAOFactory.StaticBuffDAO.InsertOrUpdateAsync(bf);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                        }
                    }
                }
                #endregion

                #region StaticBonus Save
                foreach (StaticBonusDTO bonus in character.StaticBonusList.ToArray())
                {
                    try
                    {
                        await DAOFactory.StaticBonusDAO.InsertOrUpdateAsync(bonus);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                    }
                }
                #endregion

                #region Respawn Save
                foreach (RespawnDTO resp in character.Respawns)
                {
                    try
                    {
                        if (resp.MapId != 0 && resp.X != 0 && resp.Y != 0)
                        {
                            await DAOFactory.RespawnDAO.InsertOrUpdateAsync(resp);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                    }
                }
                #endregion

                #region Quest Save
                foreach (CharacterQuestDTO q in DAOFactory.CharacterQuestDAO.LoadByCharacterId(character.CharacterId).ToList())
                {
                    DAOFactory.CharacterQuestDAO.Delete(character.CharacterId, q.QuestId);
                }

                foreach (CharacterQuest qst in character.Quests.ToList())
                {
                    try
                    {
                        CharacterQuestDTO qstDTO = new CharacterQuestDTO
                        {
                            CharacterId = qst.CharacterId,
                            QuestId = qst.QuestId,
                            FirstObjective = qst.FirstObjective,
                            SecondObjective = qst.SecondObjective,
                            ThirdObjective = qst.ThirdObjective,
                            FourthObjective = qst.FourthObjective,
                            FifthObjective = qst.FifthObjective,
                            IsMainQuest = qst.IsMainQuest
                        };
                        DAOFactory.CharacterQuestDAO.InsertOrUpdate(qstDTO);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogUserEventError("CHARACTER_DB_SAVE", character.Session.GenerateIdentity(), "ERROR", ex);
                    }
                }
                #endregion

                #region Fish Save
                try
                {
                    DAOFactory.CharacterFishDAO.InsertOrUpdateFromList(character.FishingLogs);
                }
                catch (Exception ex)
                {
                    //LOGGERServerLog(ex.ToString(), LogType.ServerError);
                }
                #endregion

                #region Battle Pass
                try
                {
                    character.SaveBattlePass();
                }
                catch (Exception ex)
                {
                    //LOGGERServerLog(ex.ToString(), LogType.ServerError);
                }
                #endregion

            }
            catch (Exception ex)
            {
                //LOGGERServerLog(ex.ToString(), LogType.ServerError);
            }

            //LOGGER($"[SAVE] {character.Name} saved");
        }
    }
}
