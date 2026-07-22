using NosGm.Packets.CustomPackets;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.HttpClients;
using NosGm.GameObject.Modules.Bazaar.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject.Extension
{
    public static class CharacterExtension
    {
        #region Methods

        public static void GenerateMSlot(ClientSession Session)
        {
            Session.SendPacket($"mslot {Session.Character.LastComboCastId} -1");
        }

        public static string GenerateFishPacket(this ClientSession s, FishPacketType type, short fishVNum, short fishLength)
        {
            var packet = $"fish {(byte)type} ";

            switch (type)
            {
                case FishPacketType.Fishing:
                    {
                        var current = s.Character.FishingLogs.FirstOrDefault(s => s.FishId == fishVNum);
                        if (current == null)
                        {
                            var log = new CharacterFishDto
                            {
                                FishCount = 1,
                                FishId = fishVNum,
                                MaxLength = fishLength,
                                CharacterId = s.Character.CharacterId
                            };

                            s.Character.FishingLogs.Add(log);
                            packet += $"{log.FishId - 10400}.{log.FishCount}.{log.MaxLength}";
                        }
                        else
                        {
                            current.FishCount += 1;

                            if (fishLength > current.MaxLength)
                            {
                                current.MaxLength = fishLength;
                            }

                            s.Character.FishingLogs.Add(current);
                            packet += $"{current.FishId - 10400}.{current.FishCount}.{current.MaxLength}";
                        }
                    }
                    break;

                case FishPacketType.Login:
                    {
                        for (int i = 0; i < 99; i++)
                        {
                            var vnum = i + 10400;
                            var fish = s.Character.FishingLogs.FirstOrDefault(s => s.FishId == vnum);

                            if (fish == null)
                            {
                                packet += $"{i}.0.0 ";
                            }
                            else
                            {
                                packet += $"{i}.{fish.FishCount}.{fish.MaxLength} ";
                            }
                        }
                        packet += "2 -1.9409.9416";
                    }
                    break;
            }

            return packet;
        }

        public static string GetFamilyNameType(this ClientSession s)
        {
            var thisRank = s.Character.FamilyCharacter.Authority;

            return thisRank == FamilyAuthority.Member ? "918" :
                thisRank == FamilyAuthority.Familydeputy ? "916" :
                thisRank == FamilyAuthority.Familykeeper ? "917" :
                thisRank == FamilyAuthority.Head ? "915" : "-1 -";
        }

        public static void SendSomePacket(this ClientSession session)
        {
            session.SendPacket("rsfi 7 1 10 10 10 10");
            session.SendPacket("sqst 0 0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            session.SendPacket("sqst 1 0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            session.SendPacket("sqst 2 0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            session.SendPacket("sqst 3 00000000000000000000000000000000000000000000000000000000000000000000000000000000000U}}t}}V}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}00U}}000000UX00000000000000000000000000000000000000000000000000000000000000000000000zW000000000000000");
            session.SendPacket("sqst 4 00UX00O}Z0009000000000UW000000000000000000000000000000UW000000000000000000000000000U}}z}}}}}}}}}}z}zV}}}l000000000000OWW0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            session.SendPacket("sqst 5 0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            session.SendPacket("sqst 6 0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        }

        public static void GetBuffFromSet(this ClientSession s, Tuple<long, long, long, short> set)
        {
            if (!s.HaveThisStuffWeared(EquipmentType.MainWeapon, set.Item1) ||
                !s.HaveThisStuffWeared(EquipmentType.SecondaryWeapon, set.Item2) ||
                !s.HaveThisStuffWeared(EquipmentType.Armor, set.Item3))
            {
                return;
            }

            if (s.Character.Buff.ContainsKey(set.Item4))
            {
                return;
            }

            s.RemoveSetBuff();
            s.Character.AddBuff(new Buff(set.Item4, 1, true), s.Character.BattleEntity);
        }

        public static void GoldLess(this ClientSession session, long value)
        {
            session.Character.Gold -= value;
            if (session.Character.Gold <= 0) session.Character.Gold = 0;

            session.SendPacket(session.Character.GenerateGold());
        }

        public static void GoldUp(this ClientSession session, long value)
        {
            session.Character.Gold += value;
            session.SendPacket(session.Character.GenerateGold());
        }

        public static bool HaveThisStuffWeared(this ClientSession s, EquipmentType type, long Vnum)
        {
            var item = s.Character.Inventory.LoadBySlotAndType((byte)type, InventoryType.Wear);
            if (item == null)
            {
                return false;
            }

            if (item.ItemVNum != Vnum)
            {
                return false;
            }

            return true;
        }

        public static void RemoveSetBuff(this ClientSession s)
        {
            s.Character.RemoveBuff(45, true);
            s.Character.RemoveBuff(46, true);
        }

        public static void SendShopEnd(this ClientSession s)
        {
            s.SendPacket("shop_end 2");
            s.SendPacket("shop_end 1");
        }

        public static string GenerateRCSList(this ClientSession s, CSListPacket packet)
        {
            return BazaarHttpClient.Instance.GenerateRcsList(new GetRcsListQuery
            {
                Model = new RcsPacketModel
                {
                    CharacterId = s.Character.CharacterId,
                    Packet = packet
                }
            });
        }

        #endregion
    }
}