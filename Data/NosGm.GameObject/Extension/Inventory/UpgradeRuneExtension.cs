using System.Collections.Generic;
using System.Linq;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using static NosGm.Domain.BCardType;

namespace NosGm.GameObject.Extension.Inventory
{
    public static class UpgradeRuneExtension
    {
        #region Constants

        public const short PREMIUM_RUNE_SCROLL = 5813;
        public const short BASIC_RUNE_SCROLL = 5823;

        #endregion

        #region Methods

        public static void UpgradeRune(this ItemInstance equipment, ClientSession session, UpgradeRuneType protectionType)
        {
            byte[] percentBroken = UpgradeRuneConfigrationExtension.PercentBroken;
            int[] goldPrice = UpgradeRuneConfigrationExtension.GoldPrice;
            byte[] percentSuccesss = UpgradeRuneConfigrationExtension.PercentSucess;
            byte[] percentFail = UpgradeRuneConfigrationExtension.PercentFail;

            if (!CanUpgrade(equipment))
            {
                session.SendShopEnd();
                return;
            }

            if (protectionType != UpgradeRuneType.None)
            {
                if (session.Character.Inventory.CountItem(protectionType == UpgradeRuneType.Premium
                    ? PREMIUM_RUNE_SCROLL
                    : BASIC_RUNE_SCROLL) < 1)
                {
                    session.SendShopEnd();
                    return;
                }
            }

            var isProtected = false;
            int value = equipment.RuneAmount;

            if (session.Character.Gold < goldPrice[value])
            {
                session.SendShopEnd();
                return;
            }

            switch (protectionType)
            {
                case UpgradeRuneType.Premium:
                case UpgradeRuneType.Basic:
                    isProtected = true;
                    break;
            }

            var probsOfBreaking = percentBroken[value];
            var probsOfFailing = percentFail[value] + (protectionType == UpgradeRuneType.Premium ? 2 : 0);

            bool upgraded = false;
            string msg;
            int effectId;
            if (ServerManager.RandomProbabilityCheck(probsOfBreaking)) // fail + level --
            {
                if (!isProtected)
                {
                    equipment.IsBreaked = true;
                    effectId = 3003;
                    msg = $"Your {equipment.Item.Name} rune upgrade failed. The rune has been broken.";
                }
                else
                {
                    effectId = 3004;
                    msg = $"Your {equipment.Item.Name} rune upgrade failed, but the scroll saved it.";
                }
            }
            else if (ServerManager.RandomProbabilityCheck(probsOfFailing)) // fail
            {
                effectId = 3004;
                msg = $"Your {equipment.Item.Name} rune upgrade failed.";
            }
            else // success
            {
                equipment.RuneAmount++;
                effectId = 3005;
                msg = $"Your {equipment.Item.Name} rune has been upgraded.";
                equipment.ApplyRandomRune(session, msg);
                upgraded = true;
            }

            ConsumeMaterials(equipment, session, protectionType, isProtected, upgraded);

            session.SendPacket(equipment.GenerateInventoryAdd());
            session.GoldLess(goldPrice[value]);
            session.CurrentMapInstance.Broadcast(
                StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, effectId),
                session.Character.PositionX, session.Character.PositionY);
            session.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            session.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            session.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, session.Character.CharacterId, 2388));
            session.SendShopEnd();
        }

        private static void ConsumeMaterials(ItemInstance equipment, ClientSession session, UpgradeRuneType protectionType, bool isProtected, bool upgraded)
        {
            var value = equipment.RuneAmount;

            if (isProtected && !upgraded)
            {
                if (ServerManager.RandomProbabilityCheck(50))
                {
                    switch (equipment.Item.ItemType)
                    {
                        case ItemType.Weapon:
                            RemoveRequiredItems(session, value, UpgradeRuneConfigrationExtension.RequiredItemWeapon);
                            break;

                        case ItemType.Armor:
                            RemoveRequiredItems(session, value, UpgradeRuneConfigrationExtension.RequiredItemArmor);
                            break;
                    }
                }
                else
                {
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        "Luckly, materials weren't consumed after this try thanks to premium scroll.", 0));
                }
            }
            else
            {
                switch (equipment.Item.ItemType)
                {
                    case ItemType.Weapon:
                        RemoveRequiredItems(session, value, UpgradeRuneConfigrationExtension.RequiredItemWeapon);
                        break;

                    case ItemType.Armor:
                        RemoveRequiredItems(session, value, UpgradeRuneConfigrationExtension.RequiredItemArmor);
                        break;
                }
            }

            switch (protectionType)
            {
                case UpgradeRuneType.Premium:
                    session.Character.Inventory.RemoveItemAmount(PREMIUM_RUNE_SCROLL);
                    break;
                case UpgradeRuneType.Basic:
                    session.Character.Inventory.RemoveItemAmount(BASIC_RUNE_SCROLL);
                    break;
            }
        }

        private static void RemoveRequiredItems(ClientSession session, int value, List<int[]> requiredItems)
        {
            var itemsToRemove = requiredItems[value - 1];

            for (int i = 0; i < itemsToRemove.Length; i += 2)
            {
                int itemId = itemsToRemove[i];
                int quantity = itemsToRemove[i + 1];

                session.Character.Inventory.RemoveItemAmount(itemId, quantity);
            }
        }

        private static void ApplyRandomRune(this ItemInstance e, ClientSession s, string msg)
        {
            switch (e.RuneAmount)
            {
                case 3:
                case 6:
                case 9:
                case 12:
                case 15:
                case 18:
                case 21:
                    e.ApplyRuneBuffMethod(s, msg);
                    break;

                default:
                    e.ApplyRuneEffect(s, msg);
                    break;
            }
        }

        private static void UpgradeOnlyBuffEffect(this ItemInstance e, ClientSession s, string msg)
        {
            var listRune = DAOFactory.RuneEffectDAO.LoadByEquipmentSerialId(e.EquipmentSerialId).Where(b => b.Type != 0).ToList();

            List<RuneEffectDTO> list = new List<RuneEffectDTO>();
            list.AddRange(listRune);

            var rndmBuff = ServerManager.RandomNumber(0, list.Count());
            RuneEffectDTO effect;
            switch (rndmBuff)
            {
                case 1:
                    effect = list[1];
                    break;

                default:
                    effect = list[0];
                    break;
            }

            if (effect == null)
            {
                return;
            }

            effect.RuneEffectId = effect.RuneEffectId;
            effect.Type++;
            effect.ThirdData++;
            DAOFactory.RuneEffectDAO.InsertOrUpdate(effect);
            e.UpdateRuneList();

            s.SendPacket(
                $"ru_suc 0 {effect.Type}.{(byte)effect.SubType}.{effect.FirstData * 4}.{effect.SecondData * 4}.{effect.ThirdData} " +
                msg);
        }

        private static void ApplyRuneBuffMethod(this ItemInstance e, ClientSession s, string msg)
        {
            var listRune = DAOFactory.RuneEffectDAO.LoadByEquipmentSerialId(e.EquipmentSerialId).Where(b => b.Type != 0);

            if (listRune.Count() == 2)
            {
                UpgradeOnlyBuffEffect(e, s, msg);
                return;
            }

            ApplyRuneBuff(e, s, msg);
        }

        private static void ApplyRuneBuff(this ItemInstance equipment, ClientSession session, string message)
        {
            var possibleBuffByTypeAndSubType = equipment.Item.ItemType == ItemType.Weapon ? GetEffectList() : GetArmorBuffList();

            var rndmBuff = ServerManager.RandomNumber(0, possibleBuffByTypeAndSubType.Length);

            var getBuff = possibleBuffByTypeAndSubType[rndmBuff];

            var runeBuff = DAOFactory.RuneEffectDAO.LoadByEquipmentSerialId(equipment.EquipmentSerialId).Where(
                    s => s.SubType == getBuff.SubType && (byte)s.Type == (byte)getBuff.Type)
                .FirstOrDefault();

            //105.2.4.7640.1 106.1.4.7720.1
            if (runeBuff != null)
            {
                if (runeBuff.ThirdData == 5)
                {
                    equipment.ApplyRuneBuffMethod(session, message);
                    return;
                }

                equipment.RuneEffects.Remove(runeBuff);
                runeBuff.ThirdData++;
                runeBuff.FirstData++;

                runeBuff = new RuneEffectDTO
                {
                    RuneEffectId = runeBuff.RuneEffectId,
                    SubType = (byte)getBuff.SubType,
                    Type = (CardType)getBuff.Type,
                    FirstData = runeBuff.FirstData,
                    SecondData = getBuff.ValueByLevel[runeBuff.ThirdData - 1],
                    ThirdData = runeBuff.ThirdData,
                    EquipmentSerialId = equipment.EquipmentSerialId,
                    IsPower = true
                };
            }
            else
            {
                if (equipment.RuneEffects.Where(x => x.Type == CardType.A7Powers1 || x.Type == CardType.A7Powers2)
                    .Count() >= 2)
                {
                    equipment.ApplyRuneBuffMethod(session, message);
                    return;
                }

                runeBuff = new RuneEffectDTO
                {
                    SubType = (byte)getBuff.SubType,
                    Type = (CardType)getBuff.Type,
                    FirstData = 1,
                    SecondData = getBuff.ValueByLevel[0],
                    ThirdData = 1,
                    EquipmentSerialId = equipment.EquipmentSerialId
                };
            }

            equipment.RuneEffects.Add(runeBuff);
            DAOFactory.RuneEffectDAO.InsertOrUpdate(runeBuff);

            session.SendPacket(
                $"ru_suc 0 {runeBuff.Type}.{(byte)runeBuff.SubType}.{runeBuff.FirstData * 4}.{runeBuff.SecondData * 4}.{runeBuff.ThirdData} " +
                message);
        }

        private static void ApplyRuneEffect(this ItemInstance equipment, ClientSession session, string message)
        {
            var possibleListTypeandSubType = equipment.Item.ItemType == ItemType.Weapon ? GetBuffList(equipment) : GetArmorEffectList();

            var rndm = ServerManager.RandomNumber(0, possibleListTypeandSubType.Length);

            var getTypeAndSubtype = possibleListTypeandSubType[rndm];

            var runeEffect = DAOFactory.RuneEffectDAO.LoadByEquipmentSerialId(equipment.EquipmentSerialId).Where(
                s => s.SubType == getTypeAndSubtype.SubType &&
                     s.Type == (CardType)getTypeAndSubtype.Type).FirstOrDefault();

            if (runeEffect != null)
            {
                runeEffect.ThirdData++;

                if (runeEffect.ThirdData == 6)
                {
                    runeEffect.ThirdData--;
                    equipment.ApplyRuneEffect(session, message);
                    return;
                }

                equipment.RuneEffects.Remove(runeEffect);

                runeEffect = new RuneEffectDTO
                {
                    RuneEffectId = runeEffect.RuneEffectId,
                    SubType = (byte)getTypeAndSubtype.SubType,
                    Type = (CardType)getTypeAndSubtype.Type,
                    FirstData = getTypeAndSubtype.ValueByLevel[runeEffect.ThirdData - 1],
                    SecondData = 0,
                    ThirdData = runeEffect.ThirdData,
                    EquipmentSerialId = equipment.EquipmentSerialId
                };
            }
            else
            {
                if (equipment.RuneEffects.Where(x => x.Type != CardType.A7Powers1 && x.Type == CardType.A7Powers2)
                    .Count() >= 7)
                {
                    equipment.ApplyRuneEffect(session, message);
                    return;
                }

                runeEffect = new RuneEffectDTO
                {
                    SubType = (byte)getTypeAndSubtype.SubType,
                    Type = (CardType)getTypeAndSubtype.Type,
                    FirstData = getTypeAndSubtype.ValueByLevel[0],
                    SecondData = 0,
                    ThirdData = 1,
                    EquipmentSerialId = equipment.EquipmentSerialId
                };
            }

            equipment.RuneEffects.Add(runeEffect);
            DAOFactory.RuneEffectDAO.InsertOrUpdate(runeEffect);

            equipment.UpdateRuneList();

            session.SendPacket(
                $"ru_suc 0 {(byte)runeEffect.Type}.{(byte)runeEffect.SubType}.{runeEffect.FirstData * 4}.{runeEffect.SecondData * 4}.{runeEffect.ThirdData} " +
                message);
        }

        private static bool CanUpgrade(ItemInstance item)
        {
            if (item.Item.EquipmentSlot != EquipmentType.MainWeapon) return false;

            if (item.RuneAmount == 15) return false;

            if (item.Item.LevelMinimum < 80 && !item.Item.IsHeroic) return false;

            if (item.IsBreaked) return false;

            return true;
        }

        // Armor bcard effect
        private static PossibleTypeAndSubtype[] GetArmorEffectList()
        {
            var possibleListTypeandSubType = new[]
            {
                new PossibleTypeAndSubtype
                {
                    Type = 13,
                    SubType = 0,//11
                    ValueByLevel = new short[] { 3, 5, 7, 10, 15, 20}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 9,
                    SubType = 0,//11
                    ValueByLevel = new short[] { 40, 80, 120, 150, 220, 300 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 10,
                    SubType = 3,//41
                    ValueByLevel = new short[] { 4, 8, 12, 16, 20, 25 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 102,
                    SubType = 1,//21
                    ValueByLevel = new short[] { 1, 2, 3, 4, 5, 6 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 33,
                    SubType = 2,//31
                    ValueByLevel = new short[] { 1, 2, 4, 7, 10, 13 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 102,
                    SubType = 3,//41
                    ValueByLevel = new short[] { 1, 2, 3, 4, 5, 6 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 33,
                    SubType = 3,//41
                    ValueByLevel = new short[] { 1, 2, 4, 7, 10, 13 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 10,
                    SubType = 0,//11
                    ValueByLevel = new short[] { 10, 20, 40, 70, 100, 150 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 101,
                    SubType = 4,//51
                    ValueByLevel = new short[] { 1, 2, 4, 6, 8, 12 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 98,
                    SubType = 0,//22
                    ValueByLevel = new short[] { 2, 4, 8, 12, 18, 25 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 5,
                    SubType = 4,//52
                    ValueByLevel = new short[] { 4, 7, 10, 15, 20, 25 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 5,
                    SubType = 3,//42
                    ValueByLevel = new short[] { 1, 3, 5, 7, 10, 15 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 45,
                    SubType = 0,//12
                    ValueByLevel = new short[] { 1, 2, 4, 6, 8, 12 }
                },
            };
            return possibleListTypeandSubType;
        }

        // Armor Buff 
        private static PossibleTypeAndSubtype[] GetArmorBuffList()
        {
            // % // 1 1 2 2 3 4
            var possibleBuffByTypeAndSubType = new[]
            {
                new PossibleTypeAndSubtype
                {
                    Type = 116,
                    SubType = 0,
                    ValueByLevel = new short[] {1960, 1961, 1962, 1963, 1964, 1965}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 116,
                    SubType = 1,
                    ValueByLevel = new short[] {1966, 1967, 1968, 1969, 1970, 1971}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 116,
                    SubType = 2,
                    ValueByLevel = new short[] {1972, 1973, 1974, 1975, 1976, 1977}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 116,
                    SubType = 3,
                    ValueByLevel = new short[] {1978, 1979, 1980, 1981, 1982, 1983}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 116,
                    SubType = 4,
                    ValueByLevel = new short[] {1984, 1985, 1986, 1987, 1988, 1989}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 117,
                    SubType = 0,
                    ValueByLevel = new short[] {1990, 1991, 1992, 1993, 1994, 1995}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 117,
                    SubType = 1,
                    ValueByLevel = new short[] {1996, 1997, 1998, 1999, 1400, 1401}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 117,
                    SubType = 2,
                    ValueByLevel = new short[] {1402, 1403, 1404, 1405, 1406, 1407}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 117,
                    SubType = 3,
                    ValueByLevel = new short[] {1408, 1409, 1410, 1411, 1412, 1413}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 117,
                    SubType = 4,
                    ValueByLevel = new short[] {1414, 1415, 1416, 1417, 1418, 1419}
                }
            };

            return possibleBuffByTypeAndSubType;
        }

        // Weapon bcard Effect
        private static PossibleTypeAndSubtype[] GetBuffList(ItemInstance e)
        {
            var possibleListTypeandSubType = new[]
            {
                new PossibleTypeAndSubtype
                {
                    Type = 3,
                    SubType = 0,//11
                    ValueByLevel = new short[] {20, 40, 80, 150, 200, 250}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 102,
                    SubType = 0, // 11
                    ValueByLevel = new short[] {1, 2, 3, 4, 5, 6}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 102,
                    SubType = 2,//31
                    ValueByLevel = new short[] {1, 2, 3, 4, 5, 6}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 44,
                    SubType = 1, // 21
                    ValueByLevel = new short[] {1, 2, 4, 7, 10, 13}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 4,
                    SubType = (short) (e.Item.Class == 8 ? 3 : 0), // 41 or 11
                    ValueByLevel = e.Item.Class == 8 ? new short[] {1, 3, 5, 7, 15, 22} : new short[] {20, 40, 70, 110, 150, 190}
                },
                new PossibleTypeAndSubtype
                {
                    Type = (short) (e.Item.Class == 8 ? 103 : 102),
                    SubType = 4, // 51
                    ValueByLevel = new short[] {1, 2, 4, 7, 10, 13}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 96,
                    SubType = 2, // 31
                    ValueByLevel = new short[] {3, 6, 9, 12, 16, 20}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 96,
                    SubType = 0,//11
                    ValueByLevel = new short[] { 3, 6, 9, 12, 16, 20 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 50,
                    SubType = 3, // 41
                    ValueByLevel = new short[] { 3, 6, 9, 12, 16, 20 }
                },
                new PossibleTypeAndSubtype
                {
                    Type = 10,
                    SubType = 4, // 51
                    ValueByLevel = new short[] { 3, 6, 9, 12, 16, 20}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 5,
                    SubType = 0,//11
                    ValueByLevel = new short[] { 1, 2, 4, 6, 8, 10}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 5,
                    SubType = 1, //21
                    ValueByLevel = new short[] { 3, 6, 9, 12, 18, 25}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 104,
                    SubType = 3, // 41
                    ValueByLevel = new short[] {1, 2, 4, 7, 10, 13}
                }
            };
            return possibleListTypeandSubType;
        }

        // Weapon BuffList
        private static PossibleTypeAndSubtype[] GetEffectList()
        {
            var possibleBuffByTypeAndSubType = new[]
            {
                new PossibleTypeAndSubtype
                {
                    Type = 105,
                    SubType = 0,
                    ValueByLevel = new short[] {1900, 1901, 1902, 1903, 1904, 1950}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 105,
                    SubType = 1,
                    ValueByLevel = new short[] {1905, 1906, 1907, 1908, 1909, 1951}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 105,
                    SubType = 2,
                    ValueByLevel = new short[] {1910, 1911, 1912, 1913, 1914, 1952}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 105,
                    SubType = 3,
                    ValueByLevel = new short[] {1915, 1916, 1917, 1918, 1919, 1953}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 105,
                    SubType = 4,
                    ValueByLevel = new short[] {1920, 1921, 1922, 1923, 1924, 1954}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 106,
                    SubType = 0,
                    ValueByLevel = new short[] {1925, 1926, 1927, 1928, 1929, 1955}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 106,
                    SubType = 1,
                    ValueByLevel = new short[] {1930, 1931, 1932, 1933, 1934, 1956}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 106,
                    SubType = 2,
                    ValueByLevel = new short[] {1935, 1936, 1937, 1938, 1939, 1957}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 106,
                    SubType = 3,
                    ValueByLevel = new short[] {1940, 1941, 1942, 1943, 1944, 1958}
                },
                new PossibleTypeAndSubtype
                {
                    Type = 106,
                    SubType = 4,
                    ValueByLevel = new short[] {1945, 1946, 1947, 1948, 1949, 1959}
                }
            };

            return possibleBuffByTypeAndSubType;
        }

       

        #endregion
    }
}