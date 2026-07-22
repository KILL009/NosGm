﻿using NosGm.Core;
using NosGm.Core.Handling;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using NosGm.GameObject.Networking;
using System.Collections.Concurrent;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core.Interfaces.Packets.ClientPackets;
using NosGm.GameObject.Helpers;
using NosGm.Handler.BasicPacket.CharScreen;
using NosGm.GameObject.Extension.Inventory;
using System.Threading.Tasks;


namespace NosGm.Handler.Packets.CharScreenPackets
{
    public static class CharCreateExtension
    {

        public static List<InventoryItem> ItemsToAdd = new List<InventoryItem>
        {
              /*   new InventoryItem { VNum = 8160, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 1906, Amount = 1, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 8011, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 8009, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 8010, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 8012, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 8162, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 4404, Amount = 1, InventoryType = InventoryType.Equipment, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 9041, Amount = 1, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 9023, Amount = 10, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 1009, Amount = 99, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 //new InventoryItem { VNum = 14003, Amount = 1, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 1012, Amount = 99, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 new InventoryItem { VNum = 9123, Amount = 1, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },
                 //new InventoryItem { VNum = 14007, Amount = 3, InventoryType = InventoryType.Main, Rare = 0, Design = 0 },*/
        };

        public static async Task GenerateInventoryCreation(this ClientSession Session, ICharacterCreatePacket characterCreatePacket, ClassType classType, CharacterDTO characterDTO, List<InventoryItem> items)
        {
            using (Inventory inventory = new Inventory(new Character(characterDTO)))
            {
                foreach (var item in items)
                {
                    await inventory.AddNewToInventoryAsync(item.VNum, item.Amount, item.InventoryType, item.Rare, item.Design);
                }
                inventory.ForEach(i => DAOFactory.ItemInstanceDAO.InsertOrUpdate(i)); ;
                new EntryPointPacketHandler(Session).LoadCharacters(new NosGmEntryPointPacket { PacketData = characterCreatePacket.OriginalContent });
            }
        }

        public async static void CreateCharacterAction(this ClientSession Session, ICharacterCreatePacket characterCreatePacket, ClassType classType)
        {
            if (Session.HasCurrentMapInstance)
            {
                return;
            }

            Logger.LogUserEvent("CREATECHARACTER", Session.GenerateIdentity(), $"[CreateCharacter]Name: {characterCreatePacket.Name} Slot: {characterCreatePacket.Slot} Gender: {characterCreatePacket.Gender} HairStyle: {characterCreatePacket.HairStyle} HairColor: {characterCreatePacket.HairColor}");

            if (characterCreatePacket.Slot <= 4
                && DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, characterCreatePacket.Slot) == null
                && characterCreatePacket.Name != null
                && (characterCreatePacket.Gender == GenderType.Male || characterCreatePacket.Gender == GenderType.Female)
                && (characterCreatePacket.HairStyle == HairStyleType.HairStyleA || (classType != ClassType.MartialArtist && characterCreatePacket.HairStyle == HairStyleType.HairStyleB))
                && Enumerable.Range(0, 10).Contains((byte)characterCreatePacket.HairColor)
                && (characterCreatePacket.Name.Length >= 4 && characterCreatePacket.Name.Length <= 16))
            {
                if (classType == ClassType.MartialArtist)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo("No disponible."));
                    return;

                    IEnumerable<CharacterDTO> characterDTOs = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId);

                    if (!characterDTOs.Any(s => s.Level >= 80))
                    {
                        return;
                    }

                    if (characterDTOs.Any(s => s.Class == ClassType.MartialArtist))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("MARTIAL_ARTIST_ALREADY_EXISTING")));
                        return;
                    }
                }

                Regex regex = new Regex(@"^[A-Za-z0-9_áéíóúÁÉÍÓÚäëïöüÄËÏÖÜ]+$");

                //Use Titan Shield here
                var BlackListed = new List<string>
                {
                "[",
                "]",
                "[gm]",
                "[supporter]",
                "bitch",
                "ass",
                "Dupe",
                "Exploit",
                "Steve",
                };

                if (BlackListed.Any(s => characterCreatePacket.Name.ToLower().Contains(s)))
                {
                    Session.SendPacketFormat($"info This Name has been blacklisted");
                    return;
                }

                if (regex.Matches(characterCreatePacket.Name).Count != 1)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INVALID_CHARNAME")));
                    return;
                }

                if (DAOFactory.CharacterDAO.LoadByName(characterCreatePacket.Name) != null)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CHARNAME_ALREADY_TAKEN")));
                    return;
                }

                CharacterDTO characterDTO = new CharacterDTO
                {
                    AccountId = Session.Account.AccountId,
                    Slot = characterCreatePacket.Slot,
                    Class = classType,
                    Gender = characterCreatePacket.Gender,
                    HairStyle = characterCreatePacket.HairStyle,
                    HairColor = characterCreatePacket.HairColor,
                    Name = characterCreatePacket.Name,
                    MapId = 1,
                    MapX = 80,
                    MapY = 115,
                    MaxMateCount = 10,
                    MaxPartnerCount = 4,
                    SpPoint = 10000,
                    SpAdditionPoint = 0,
                    MinilandMessage = (Language.Instance.GetMessageFromKey("MINILAND_WELCOME_MESSAGE")),
                    State = CharacterState.Active,
                    MinilandPoint = 2000,
                    Reputation = 0,
                    IsPartnerAutoRelive = true,
                    IsPetAutoRelive = true,
                    
                };

                switch (characterDTO.Class)
                {
                    case ClassType.MartialArtist:
                        {
                            characterDTO.Level = 81;
                            characterDTO.JobLevel = 50;
                            characterDTO.Hp = 9401;
                            characterDTO.Mp = 3156;
                        }
                        break;

                    default:
                        {
                            characterDTO.Level = 1;
                            characterDTO.JobLevel = 1;
                            characterDTO.Hp = 221;
                            characterDTO.Mp = 69;
                        }
                        break;
                }

                await DAOFactory.CharacterDAO.InsertOrUpdate(characterDTO);

                if (classType != ClassType.MartialArtist)
                {
                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 200 });
                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 201 });
                    DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 209 });

                    await GenerateInventoryCreation(Session, characterCreatePacket, classType, characterDTO, ItemsToAdd);
                    //await //LOGGERServerLog($"{characterCreatePacket.Name} has been created | Slot: {characterCreatePacket.Slot}", LogType.ServerInfo);

                    //var firstQuest = new CharacterQuestDTO
                    //{
                    //    CharacterId = characterDTO.CharacterId,
                    //    QuestId = 1997,
                    //    IsMainQuest = true
                    //};
                    //DAOFactory.CharacterQuestDAO.InsertOrUpdate(firstQuest);

                }
                //Martial Artist
                else
                {
                    DAOFactory.CharacterQuestDAO.InsertOrUpdate(new CharacterQuestDTO
                    {
                        CharacterId = characterDTO.CharacterId,
                        QuestId = 6275,
                        IsMainQuest = false
                    });
                    {
                        DAOFactory.CharacterQuestDAO.InsertOrUpdate(new CharacterQuestDTO
                        {
                            CharacterId = characterDTO.CharacterId,
                            QuestId = 3340,
                            IsMainQuest = true
                        });

                        for (short skillVNum = 1525; skillVNum <= 1539; skillVNum++)
                        {
                            DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO
                            {
                                CharacterId = characterDTO.CharacterId,
                                SkillVNum = skillVNum
                            });
                        }

                        DAOFactory.CharacterSkillDAO.InsertOrUpdate(new CharacterSkillDTO { CharacterId = characterDTO.CharacterId, SkillVNum = 1565 });

                        using (Inventory inventory = new Inventory(new Character(characterDTO)))
                        {
                            inventory.AddNewToInventory(5832, 1, InventoryType.Main, 5);
                            inventory.AddNewToInventory(9319, 1, InventoryType.Main);
                            inventory.AddNewToInventory(1012, 99, InventoryType.Etc);
                            inventory.AddNewToInventory(4340, 2, InventoryType.Main);
                            inventory.AddNewToInventory(1, 1, InventoryType.Main);
                            inventory.ForEach(i => DAOFactory.ItemInstanceDAO.InsertOrUpdate(i));
                            new EntryPointPacketHandler(Session).LoadCharacters(new NosGmEntryPointPacket { PacketData = characterCreatePacket.OriginalContent });
                        }
                    }
                }
            }
        }
    }
}