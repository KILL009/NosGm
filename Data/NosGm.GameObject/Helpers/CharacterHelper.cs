using NosGm.Algorithm;
using NosGm.Configuration;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.ItemThread;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindowsFirewallHelper.Addresses;

namespace NosGm.GameObject.Helpers
{
    public class CharacterHelper
    {
        #region Instantiation

        public CharacterHelper()
        {
            loadStats();
            loadSpeedData();
            loadJobXPData();
            loadSPXPData();
            loadHeroXpData();
            loadXPData();
        }

        #endregion

        #region Members

        private static int[,] _criticalDist;

        private static int[,] _criticalDistRate;

        private static int[,] _criticalHit;

        private static int[,] _criticalHitRate;

        private static int[,] _distDef;

        private static int[,] _distDodge;

        private static int[,] _distRate;

        private static double[] _firstJobXpData;

        private static double[] _heroXpData;

        private static int[,] _hitDef;

        private static int[,] _hitDodge;

        private static int[,] _hitRate;

        private static int[,] _hp;

        private static int[] _hpHealth;

        private static int[] _hpHealthStand;

        private static int[,] _magicalDef;

        private static int[,] _maxDist;

        private static int[,] _maxHit;

        private static int[,] _minDist;

        // difference between class
        private static int[,] _minHit;

        private static int[,] _mp;

        private static int[] _mpHealth;

        private static int[] _mpHealthStand;

        private static double[] _secondjobxpData;

        // STAT DATA
        private static byte[] _speedData;

        private static double[] _spxpData;

        // same for all class
        private static double[] _xpData;

        #endregion

        #region Properties
        public static double[] FirstJobXPData
        {
            get
            {
                if (_firstJobXpData == null) new CharacterHelper();
                return _firstJobXpData;
            }
        }

        public static double[] HeroXpData
        {
            get
            {
                if (_heroXpData == null) new CharacterHelper();
                return _heroXpData;
            }
        }

        public static int[,] HPData
        {
            get
            {
                if (_hp == null) new CharacterHelper();
                return _hp;
            }
        }

        public static int[] HPHealth
        {
            get
            {
                if (_hpHealth == null) new CharacterHelper();
                return _hpHealth;
            }
        }

        public static int[] HPHealthStand
        {
            get
            {
                if (_hpHealthStand == null) new CharacterHelper();
                return _hpHealthStand;
            }
        }

        public static int[,] MPData
        {
            get
            {
                if (_mp == null) new CharacterHelper();
                return _mp;
            }
        }

        public static int[] MPHealth
        {
            get
            {
                if (_mpHealth == null) new CharacterHelper();
                return _mpHealth;
            }
        }

        public static int[] MPHealthStand
        {
            get
            {
                if (_mpHealthStand == null) new CharacterHelper();
                return _mpHealthStand;
            }
        }

        public static double[] SecondJobXPData
        {
            get
            {
                if (_secondjobxpData == null) new CharacterHelper();
                return _secondjobxpData;
            }
        }

        public static byte[] SpeedData
        {
            get
            {
                if (_speedData == null) new CharacterHelper();
                return _speedData;
            }
        }

        public static double[] SPXPData
        {
            get
            {
                if (_spxpData == null) new CharacterHelper();
                return _spxpData;
            }
        }

        public static double[] XPData
        {
            get
            {
                if (_xpData == null) new CharacterHelper();
                return _xpData;
            }
        }

        #endregion

        #region Methods

        #region SP Points

        public static void UpdateSPPoints(ref ItemInstance specialistInstance, ClientSession session)
        {
            var mainWeapon =
                       session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                           InventoryType.Wear);
            var secondaryWeapon =
                session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon,
                    InventoryType.Wear);

            var effects = new List<ShellEffectDTO>();
            if (mainWeapon?.ShellEffects != null)
            {
                effects.AddRange(mainWeapon.ShellEffects);
            }

            if (secondaryWeapon?.ShellEffects != null)
            {
                effects.AddRange(secondaryWeapon.ShellEffects);
            }

            int GetShellWeaponEffectValue(ShellWeaponEffectType effectType)
            {
                return effects.Where(s => s.Effect == (byte)effectType).OrderByDescending(s => s.Value)
                           .FirstOrDefault()?.Value ?? 0;
            }

            int GetTitleEffectValue(BCardType.CardType type, byte subtype)
            {
                return session.Character.EffectFromTitle?.Where(x => x.Type == (byte)type && x.SubType == subtype)
                    ?.Sum(x => x.FirstData) ?? 0;
            }

            var slElement = SlPoint(specialistInstance.SlElement, 2)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLElement)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                            + GetTitleEffectValue(BCardType.CardType.IncreaseSlPoint,
                                (byte)AdditionalTypes.IncreaseSlPoint.IncreaseEllement);
            var slHp = SlPoint(specialistInstance.SlHP, 3)
                       + GetShellWeaponEffectValue(ShellWeaponEffectType.SLHP)
                       + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                       + GetTitleEffectValue(BCardType.CardType.IncreaseSlPoint,
                           (byte)AdditionalTypes.IncreaseSlPoint.IncreaseHPMP);
            var slDefence = SlPoint(specialistInstance.SlDefence, 1)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDefence)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                            + GetTitleEffectValue(BCardType.CardType.IncreaseSlPoint,
                                (byte)AdditionalTypes.IncreaseSlPoint.IncreaseDefence);
            var slHit = SlPoint(specialistInstance.SlDamage, 0)
                        + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDamage)
                        + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                        + GetTitleEffectValue(BCardType.CardType.IncreaseSlPoint,
                            (byte)AdditionalTypes.IncreaseSlPoint.IncreaseDamage);

            #region slHit

            specialistInstance.DamageMinimum = 0;
            specialistInstance.DamageMaximum = 0;
            specialistInstance.HitRate = 0;
            specialistInstance.CriticalLuckRate = 0;
            specialistInstance.CriticalRate = 0;
            specialistInstance.DefenceDodge = 0;
            specialistInstance.DistanceDefenceDodge = 0;
            specialistInstance.ElementRate = 0;
            specialistInstance.DarkResistance = 0;
            specialistInstance.LightResistance = 0;
            specialistInstance.FireResistance = 0;
            specialistInstance.WaterResistance = 0;
            specialistInstance.CriticalDodge = 0;
            specialistInstance.CloseDefence = 0;
            specialistInstance.DistanceDefence = 0;
            specialistInstance.MagicDefence = 0;
            specialistInstance.HP = 0;
            specialistInstance.MP = 0;

            #endregion

            #region slDefence

            if (slDefence >= 10)
            {
                specialistInstance.DefenceDodge += 5;
                specialistInstance.DistanceDefenceDodge += 5;
            }

            if (slDefence >= 20)
            {
                specialistInstance.CriticalDodge += 2;
            }

            if (slDefence >= 30)
            {
                specialistInstance.HP += 100;
            }

            if (slDefence >= 40)
            {
                specialistInstance.CriticalDodge += 2;
            }

            if (slDefence >= 50)
            {
                specialistInstance.DefenceDodge += 5;
                specialistInstance.DistanceDefenceDodge += 5;
            }

            if (slDefence >= 60)
            {
                specialistInstance.HP += 200;
            }

            if (slDefence >= 70)
            {
                specialistInstance.CriticalDodge += 3;
            }

            if (slDefence >= 75)
            {
                specialistInstance.FireResistance += 2;
                specialistInstance.WaterResistance += 2;
                specialistInstance.LightResistance += 2;
                specialistInstance.DarkResistance += 2;
            }

            if (slDefence >= 80)
            {
                specialistInstance.DefenceDodge += 10;
                specialistInstance.DistanceDefenceDodge += 10;
                specialistInstance.CriticalDodge += 3;
            }

            if (slDefence >= 90)
            {
                specialistInstance.FireResistance += 3;
                specialistInstance.WaterResistance += 3;
                specialistInstance.LightResistance += 3;
                specialistInstance.DarkResistance += 3;
            }

            if (slDefence >= 95)
            {
                specialistInstance.HP += 300;
            }

            if (slDefence >= 100)
            {
                specialistInstance.DefenceDodge += 20;
                specialistInstance.DistanceDefenceDodge += 20;
                specialistInstance.FireResistance += 5;
                specialistInstance.WaterResistance += 5;
                specialistInstance.LightResistance += 5;
                specialistInstance.DarkResistance += 5;
            }

            #endregion

            #region slHp

            if (slHp >= 5)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 10)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 15)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 20)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
                specialistInstance.CloseDefence += 10;
                specialistInstance.DistanceDefence += 10;
                specialistInstance.MagicDefence += 10;
            }

            if (slHp >= 25)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 30)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 35)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
            }

            if (slHp >= 40)
            {
                specialistInstance.DamageMinimum += 5;
                specialistInstance.DamageMaximum += 5;
                specialistInstance.CloseDefence += 15;
                specialistInstance.DistanceDefence += 15;
                specialistInstance.MagicDefence += 15;
            }

            if (slHp >= 45)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
            }

            if (slHp >= 50)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
                specialistInstance.FireResistance += 2;
                specialistInstance.WaterResistance += 2;
                specialistInstance.LightResistance += 2;
                specialistInstance.DarkResistance += 2;
            }

            if (slHp >= 55)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
            }

            if (slHp >= 60)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
            }

            if (slHp >= 65)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
            }

            if (slHp >= 70)
            {
                specialistInstance.DamageMinimum += 10;
                specialistInstance.DamageMaximum += 10;
                specialistInstance.CloseDefence += 20;
                specialistInstance.DistanceDefence += 20;
                specialistInstance.MagicDefence += 20;
            }

            if (slHp >= 75)
            {
                specialistInstance.DamageMinimum += 15;
                specialistInstance.DamageMaximum += 15;
            }

            if (slHp >= 80)
            {
                specialistInstance.DamageMinimum += 15;
                specialistInstance.DamageMaximum += 15;
            }

            if (slHp >= 85)
            {
                specialistInstance.DamageMinimum += 15;
                specialistInstance.DamageMaximum += 15;
                specialistInstance.CriticalDodge++;
            }

            if (slHp >= 86)
            {
                specialistInstance.CriticalDodge++;
            }

            if (slHp >= 87)
            {
                specialistInstance.CriticalDodge++;
            }

            if (slHp >= 88)
            {
                specialistInstance.CriticalDodge++;
            }

            if (slHp >= 90)
            {
                specialistInstance.DamageMinimum += 15;
                specialistInstance.DamageMaximum += 15;
                specialistInstance.CloseDefence += 25;
                specialistInstance.DistanceDefence += 25;
                specialistInstance.MagicDefence += 25;
            }

            if (slHp >= 91)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 92)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 93)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 94)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 95)
            {
                specialistInstance.DamageMinimum += 20;
                specialistInstance.DamageMaximum += 20;
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 96)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 97)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 98)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 99)
            {
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
            }

            if (slHp >= 100)
            {
                specialistInstance.FireResistance += 3;
                specialistInstance.WaterResistance += 3;
                specialistInstance.LightResistance += 3;
                specialistInstance.DarkResistance += 3;
                specialistInstance.CloseDefence += 30;
                specialistInstance.DistanceDefence += 30;
                specialistInstance.MagicDefence += 30;
                specialistInstance.DamageMinimum += 20;
                specialistInstance.DamageMaximum += 20;
                specialistInstance.DefenceDodge += 2;
                specialistInstance.DistanceDefenceDodge += 2;
                specialistInstance.CriticalDodge++;
            }

            #endregion

            #region slElement

            if (slElement >= 1)
            {
                specialistInstance.ElementRate += 2;
            }

            if (slElement >= 10)
            {
                specialistInstance.MP += 100;
            }

            if (slElement >= 20)
            {
                specialistInstance.MagicDefence += 5;
            }

            if (slElement >= 30)
            {
                specialistInstance.FireResistance += 2;
                specialistInstance.WaterResistance += 2;
                specialistInstance.LightResistance += 2;
                specialistInstance.DarkResistance += 2;
                specialistInstance.ElementRate += 2;
            }

            if (slElement >= 40)
            {
                specialistInstance.MP += 100;
            }

            if (slElement >= 50)
            {
                specialistInstance.MagicDefence += 5;
            }

            if (slElement >= 60)
            {
                specialistInstance.FireResistance += 3;
                specialistInstance.WaterResistance += 3;
                specialistInstance.LightResistance += 3;
                specialistInstance.DarkResistance += 3;
                specialistInstance.ElementRate += 2;
            }

            if (slElement >= 70)
            {
                specialistInstance.MP += 100;
            }

            if (slElement >= 80)
            {
                specialistInstance.MagicDefence += 5;
            }

            if (slElement >= 90)
            {
                specialistInstance.FireResistance += 4;
                specialistInstance.WaterResistance += 4;
                specialistInstance.LightResistance += 4;
                specialistInstance.DarkResistance += 4;
                specialistInstance.ElementRate += 2;
            }

            if (slElement >= 100)
            {
                specialistInstance.FireResistance += 6;
                specialistInstance.WaterResistance += 6;
                specialistInstance.LightResistance += 6;
                specialistInstance.DarkResistance += 6;
                specialistInstance.MagicDefence += 5;
                specialistInstance.MP += 200;
                specialistInstance.ElementRate += 2;
            }

            #endregion

            session.SendPackets(session.Character.GenerateStatChar());
            session.SendPacket(session.Character.GenerateStat());

        }
        #endregion

        #region Specialist Wings Buff
        public static void AddSpecialistWingsBuff(ClientSession session)
        {
            var specialist = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

            if (!session.Character.UseSp || specialist == null) return;

            WingsThread.RemoveBuff(session);
            switch (specialist.WingBuff)
            {
                case (int)WingType.AngelWings:  WingsThread.ApplyBuff(session, WingBuffType.AngelWings); break;
                case (int)WingType.DemonWings:  WingsThread.ApplyBuff(session, WingBuffType.AngelWings); break;
                case (int)WingType.FireWings:  WingsThread.ApplyBuff(session, WingBuffType.AngelWings); break;
                case (int)WingType.IceWings:  WingsThread.ApplyBuff(session, WingBuffType.AngelWings); break;
                case (int)WingType.GoldenEagleWings:  WingsThread.ApplyBuff(session, WingBuffType.GoldenEagleWings); break;
                case (int)WingType.TitanWings:  WingsThread.ApplyBuff(session, WingBuffType.TitanWings); break;
                case (int)WingType.ArchangelWings:  WingsThread.ApplyBuff(session, WingBuffType.ArchangelWings); break;
                case (int)WingType.ArchdemonWings:  WingsThread.ApplyBuff(session, WingBuffType.ArchdemonWings); break;
                case (int)WingType.BlazingFireWings:  WingsThread.ApplyBuff(session, WingBuffType.BlazingFireWings); break;
                case (int)WingType.FrostyIceWings:  WingsThread.ApplyBuff(session, WingBuffType.FrostyIceWings); break;
                case (int)WingType.GoldenWings:  WingsThread.ApplyBuff(session, WingBuffType.GoldenWings); break;
                case (int)WingType.OnyxWings:  WingsThread.ApplyBuff(session, WingBuffType.OnyxWings); break;
                case (int)WingType.FairyWings:  WingsThread.ApplyBuff(session, WingBuffType.FairyWings); break;
                case (int)WingType.MegaTitanWings:  WingsThread.ApplyBuff(session, WingBuffType.MegaTitanWings); break;
                case (int)WingType.ZephyrWings:  WingsThread.ApplyBuff(session, WingBuffType.ZephyrWings); break;
                case (int)WingType.LightningWings:  WingsThread.ApplyBuff(session, WingBuffType.LightningWings); break;
                case (int)WingType.BladeWings:  WingsThread.ApplyBuff(session, WingBuffType.BladeWings); break;
                case (int)WingType.CrystalWings:  WingsThread.ApplyBuff(session, WingBuffType.CrystalWings); break;
                case (int)WingType.PetalWings:  WingsThread.ApplyBuff(session, WingBuffType.PetalWings); break;
                case (int)WingType.LunarWings:  WingsThread.ApplyBuff(session, WingBuffType.LunarWings); break;
                case (int)WingType.GreenRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.PinkRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.YellowRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.PurpleRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.RedRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.MagentaRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.CyanRetroWings:  WingsThread.ApplyBuff(session, WingBuffType.RetroWings); break;
                case (int)WingType.TreeWings:  WingsThread.ApplyBuff(session, WingBuffType.TreeWings); break;
            }
        }
        #endregion

        #region Authority Chat Color
        public static byte AuthorityChatColor(AuthorityType authority)
        {
            switch (authority)
            {
                case AuthorityType.GS:
                    return 17;

                case AuthorityType.GM:
                case AuthorityType.ADMIN:
                case AuthorityType.DEV:
                    return 12;

                default:
                    return 0;
            }
        }
        #endregion

        #region Experience Penality
        public static float ExperiencePenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 6:
                    penalty = 0.9f;
                    break;

                case 7:
                    penalty = 0.7f;
                    break;

                case 8:
                    penalty = 0.5f;
                    break;

                case 9:
                    penalty = 0.3f;
                    break;

                default:
                    if (leveldifference > 9)
                        penalty = 0.1f;
                    else if (leveldifference > 18)
                        penalty = 0.05f;
                    else
                        penalty = 1f;
                    break;
            }

            return penalty;
        }
        #endregion

        #region Gold Penality

        public static float GoldPenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 5:
                    penalty = 0.9f;
                    break;

                case 6:
                    penalty = 0.7f;
                    break;

                case 7:
                    penalty = 0.5f;
                    break;

                case 8:
                    penalty = 0.3f;
                    break;

                case 9:
                    penalty = 0.2f;
                    break;

                default:
                    if (leveldifference > 9 && leveldifference < 19)
                        penalty = 0.1f;
                    else if (leveldifference > 18 && leveldifference < 30)
                        penalty = 0.05f;
                    else if (leveldifference > 30)
                        penalty = 0f;
                    else
                        penalty = 1f;
                    break;
            }

            return penalty;
        }
        #endregion

        #region Load Data
        public static long LoadFairyXPData(long elementRate, short startLevel = 40)
        {
            #region Calculation
            /*
            Below 40%
            CurrentLevel * CurrentLevel + StartLevel = MonsterNeeded
            Above 40%
            CurrentLevel * CurrentLevel * 3 + StartLevel = MonsterNeeded
            */
            #endregion

            if (elementRate < 40)
                return elementRate * elementRate * GameConfiguration.LowerFairyFactor + startLevel;
            return elementRate * elementRate * GameConfiguration.HigherFairyFactor + startLevel;
        }

        public static int LoadFamilyXPData(byte familyLevel)
        {
            switch (familyLevel)
            {
                case 1:
                    return FamilySystemConfiguration.Level2XPNeeded;

                case 2:
                    return 250000;

                case 3:
                    return 370000;

                case 4:
                    return 560000;

                case 5:
                    return 840000;

                case 6:
                    return 1260000;

                case 7:
                    return 1900000;

                case 8:
                    return 2850000;

                case 9:
                    return 3570000;

                case 10:
                    return 3830000;

                case 11:
                    return 4150000;

                case 12:
                    return 4750000;

                case 13:
                    return 5500000;

                case 14:
                    return 6500000;

                case 15:
                    return 7000000;

                case 16:
                    return 8500000;

                case 17:
                    return 9500000;

                case 18:
                    return 10000000;

                case 19:
                    return 17000000;

                default:
                    return 999999999;
            }
        }
        #endregion

        #region Min/Max
        public static int MagicalDefence(ClassType @class, byte level)
        {
            if (_magicalDef == null)
            {
                new CharacterHelper();
            }
            return _magicalDef[(int)@class, level]; ;
        }

        public static int MaxDistance(ClassType @class, byte level)
        {
            if (_maxDist == null)
            {
                new CharacterHelper();
            }
            return _maxDist[(int)@class, level]; ;
        }

        public static int MaxHit(ClassType @class, byte level)
        {
            if (_maxHit == null)
            {
                new CharacterHelper();
            }
            return _maxHit[(int)@class, level]; ;
        }

        public static int MinDistance(ClassType @class, byte level)
        {
            if (_minDist == null)
            {
                new CharacterHelper();
            }
            return _minDist[(int)@class, level]; ;
        }

        public static int MinHit(ClassType @class, byte level)
        {
            if (_minHit == null)
            {
                new CharacterHelper();
            }
            return _minHit[(int)@class, level]; ;
        }
        #endregion

        public static int RarityPoint(short rarity, short lvl, bool armor)
        {
            int p;
            switch (rarity)
            {
                case 0:
                    p = 0;
                    break;

                case 1:
                    p = 1;
                    break;

                case 2:
                    p = 2;
                    break;

                case 3:
                    p = 3;
                    break;

                case 4:
                    p = 4;
                    break;

                case 5:
                    p = 5;
                    break;

                case 6:
                    p = 7;
                    break;

                case 7:
                    p = 10;
                    break;

                case 8:
                    p = 15;
                    break;

                default:
                    p = rarity * 2;
                    break;
            }

            return p * (lvl / (armor ? 10 : 5) + 1);
        }

        public static void RemoveDragonBuff(ClientSession session)
        {
            session.Character.RemoveBuff(942, true);
            session.Character.RemoveBuff(943, true);
            session.Character.RemoveBuff(944, true);
            session.Character.RemoveBuff(945, true);
            session.Character.RemoveBuff(946, true);
        }

        public static int SlPoint(short spPoint, short mode)
        {
            try
            {
                int point = 0;
                switch (mode)
                {
                    case 0:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 28)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 88)
                        {
                            point = 19 + ((spPoint - 28) / 3);
                        }
                        else if (spPoint <= 168)
                        {
                            point = 39 + ((spPoint - 88) / 4);
                        }
                        else if (spPoint <= 268)
                        {
                            point = 59 + ((spPoint - 168) / 5);
                        }
                        else if (spPoint <= 334)
                        {
                            point = 79 + ((spPoint - 268) / 6);
                        }
                        else if (spPoint <= 383)
                        {
                            point = 90 + ((spPoint - 334) / 7);
                        }
                        else if (spPoint <= 391)
                        {
                            point = 97 + ((spPoint - 383) / 8);
                        }
                        else if (spPoint <= 400)
                        {
                            point = 98 + ((spPoint - 391) / 9);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + ((spPoint - 400) / 10);
                        }
                        else if (spPoint <= 413)
                        {
                            point = 100 + ((spPoint - 403) / 10);
                        }
                        else if (spPoint <= 416)
                        {
                            point = 101 + ((spPoint - 406) / 10);
                        }
                        else if (spPoint <= 419)
                        {
                            point = 102 + ((spPoint - 409) / 10);
                        }
                        else if (spPoint <= 422)
                        {
                            point = 103 + ((spPoint - 412) / 10);
                        }
                        else if (spPoint <= 425)
                        {
                            point = 104 + ((spPoint - 415) / 10);
                        }
                        else if (spPoint <= 429)
                        {
                            point = 105 + ((spPoint - 419) / 10);
                        }
                        else if (spPoint <= 433)
                        {
                            point = 106 + ((spPoint - 423) / 10);
                        }
                        else if (spPoint <= 437)
                        {
                            point = 107 + ((spPoint - 427) / 10);
                        }
                        else if (spPoint <= 441)
                        {
                            point = 108 + ((spPoint - 431) / 10);
                        }
                        else if (spPoint <= 445)
                        {
                            point = 109 + ((spPoint - 435) / 10);
                        }
                        else if (spPoint <= 449)
                        {
                            point = 110 + ((spPoint - 439) / 10);
                        }
                        else if (spPoint <= 453)
                        {
                            point = 111 + ((spPoint - 443) / 10);
                        }
                        else if (spPoint <= 457)
                        {
                            point = 112 + ((spPoint - 447) / 10);
                        }
                        else if (spPoint <= 461)
                        {
                            point = 113 + ((spPoint - 451) / 10);
                        }
                        else if (spPoint <= 465)
                        {
                            point = 114 + ((spPoint - 455) / 10);
                        }
                        else if (spPoint <= 469)
                        {
                            point = 115 + ((spPoint - 459) / 10);
                        }
                        else if (spPoint <= 473)
                        {
                            point = 116 + ((spPoint - 463) / 10);
                        }
                        else if (spPoint <= 477)
                        {
                            point = 117 + ((spPoint - 467) / 10);
                        }
                        else if (spPoint <= 481)
                        {
                            point = 118 + ((spPoint - 471) / 10);
                        }
                        else if (spPoint <= 486)
                        {
                            point = 119 + ((spPoint - 476) / 10);
                        }

                        break;

                    case 1:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 48)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 81)
                        {
                            point = 29 + ((spPoint - 48) / 3);
                        }
                        else if (spPoint <= 161)
                        {
                            point = 40 + ((spPoint - 81) / 4);
                        }
                        else if (spPoint <= 236)
                        {
                            point = 60 + ((spPoint - 161) / 5);
                        }
                        else if (spPoint <= 290)
                        {
                            point = 75 + ((spPoint - 236) / 6);
                        }
                        else if (spPoint <= 360)
                        {
                            point = 84 + ((spPoint - 290) / 7);
                        }
                        else if (spPoint <= 400)
                        {
                            point = 97 + ((spPoint - 360) / 8);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + ((spPoint - 400) / 10);
                        }
                        else if (spPoint <= 413)
                        {
                            point = 100 + ((spPoint - 403) / 10);
                        }
                        else if (spPoint <= 416)
                        {
                            point = 101 + ((spPoint - 406) / 10);
                        }
                        else if (spPoint <= 419)
                        {
                            point = 102 + ((spPoint - 409) / 10);
                        }
                        else if (spPoint <= 422)
                        {
                            point = 103 + ((spPoint - 412) / 10);
                        }
                        else if (spPoint <= 425)
                        {
                            point = 104 + ((spPoint - 415) / 10);
                        }
                        else if (spPoint <= 429)
                        {
                            point = 105 + ((spPoint - 419) / 10);
                        }
                        else if (spPoint <= 433)
                        {
                            point = 106 + ((spPoint - 423) / 10);
                        }
                        else if (spPoint <= 437)
                        {
                            point = 107 + ((spPoint - 427) / 10);
                        }
                        else if (spPoint <= 441)
                        {
                            point = 108 + ((spPoint - 431) / 10);
                        }
                        else if (spPoint <= 445)
                        {
                            point = 109 + ((spPoint - 435) / 10);
                        }
                        else if (spPoint <= 449)
                        {
                            point = 110 + ((spPoint - 439) / 10);
                        }
                        else if (spPoint <= 453)
                        {
                            point = 111 + ((spPoint - 443) / 10);
                        }
                        else if (spPoint <= 457)
                        {
                            point = 112 + ((spPoint - 447) / 10);
                        }
                        else if (spPoint <= 461)
                        {
                            point = 113 + ((spPoint - 451) / 10);
                        }
                        else if (spPoint <= 465)
                        {
                            point = 114 + ((spPoint - 455) / 10);
                        }
                        else if (spPoint <= 469)
                        {
                            point = 115 + ((spPoint - 459) / 10);
                        }
                        else if (spPoint <= 473)
                        {
                            point = 116 + ((spPoint - 463) / 10);
                        }
                        else if (spPoint <= 477)
                        {
                            point = 117 + ((spPoint - 467) / 10);
                        }
                        else if (spPoint <= 481)
                        {
                            point = 118 + ((spPoint - 471) / 10);
                        }
                        else if (spPoint <= 486)
                        {
                            point = 119 + ((spPoint - 476) / 10);
                        }

                        break;

                    case 2:
                        if (spPoint <= 20)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 40)
                        {
                            point = 20 + ((spPoint - 20) / 2);
                        }
                        else if (spPoint <= 70)
                        {
                            point = 30 + ((spPoint - 40) / 3);
                        }
                        else if (spPoint <= 110)
                        {
                            point = 40 + ((spPoint - 70) / 4);
                        }
                        else if (spPoint <= 210)
                        {
                            point = 50 + ((spPoint - 110) / 5);
                        }
                        else if (spPoint <= 270)
                        {
                            point = 70 + ((spPoint - 210) / 6);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 80 + ((spPoint - 270) / 7);
                        }
                        else if (spPoint <= 413)
                        {
                            point = 100 + ((spPoint - 403) / 10);
                        }
                        else if (spPoint <= 416)
                        {
                            point = 101 + ((spPoint - 406) / 10);
                        }
                        else if (spPoint <= 419)
                        {
                            point = 102 + ((spPoint - 409) / 10);
                        }
                        else if (spPoint <= 422)
                        {
                            point = 103 + ((spPoint - 412) / 10);
                        }
                        else if (spPoint <= 425)
                        {
                            point = 104 + ((spPoint - 415) / 10);
                        }
                        else if (spPoint <= 429)
                        {
                            point = 105 + ((spPoint - 419) / 10);
                        }
                        else if (spPoint <= 433)
                        {
                            point = 106 + ((spPoint - 423) / 10);
                        }
                        else if (spPoint <= 437)
                        {
                            point = 107 + ((spPoint - 427) / 10);
                        }
                        else if (spPoint <= 441)
                        {
                            point = 108 + ((spPoint - 431) / 10);
                        }
                        else if (spPoint <= 445)
                        {
                            point = 109 + ((spPoint - 435) / 10);
                        }
                        else if (spPoint <= 449)
                        {
                            point = 110 + ((spPoint - 439) / 10);
                        }
                        else if (spPoint <= 453)
                        {
                            point = 111 + ((spPoint - 443) / 10);
                        }
                        else if (spPoint <= 457)
                        {
                            point = 112 + ((spPoint - 447) / 10);
                        }
                        else if (spPoint <= 461)
                        {
                            point = 113 + ((spPoint - 451) / 10);
                        }
                        else if (spPoint <= 465)
                        {
                            point = 114 + ((spPoint - 455) / 10);
                        }
                        else if (spPoint <= 469)
                        {
                            point = 115 + ((spPoint - 459) / 10);
                        }
                        else if (spPoint <= 473)
                        {
                            point = 116 + ((spPoint - 463) / 10);
                        }
                        else if (spPoint <= 477)
                        {
                            point = 117 + ((spPoint - 467) / 10);
                        }
                        else if (spPoint <= 481)
                        {
                            point = 118 + ((spPoint - 471) / 10);
                        }
                        else if (spPoint <= 486)
                        {
                            point = 119 + ((spPoint - 476) / 10);
                        }

                        break;

                    case 3:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 50)
                        {
                            point = 10 + ((spPoint - 10) / 2);
                        }
                        else if (spPoint <= 110)
                        {
                            point = 30 + ((spPoint - 50) / 3);
                        }
                        else if (spPoint <= 150)
                        {
                            point = 50 + ((spPoint - 110) / 4);
                        }
                        else if (spPoint <= 200)
                        {
                            point = 60 + ((spPoint - 150) / 5);
                        }
                        else if (spPoint <= 260)
                        {
                            point = 70 + ((spPoint - 200) / 6);
                        }
                        else if (spPoint <= 330)
                        {
                            point = 80 + ((spPoint - 260) / 7);
                        }
                        else if (spPoint <= 410)
                        {
                            point = 90 + ((spPoint - 330) / 8);
                        }
                        else if (spPoint <= 413)
                        {
                            point = 100 + ((spPoint - 403) / 10);
                        }
                        else if (spPoint <= 416)
                        {
                            point = 101 + ((spPoint - 406) / 10);
                        }
                        else if (spPoint <= 419)
                        {
                            point = 102 + ((spPoint - 409) / 10);
                        }
                        else if (spPoint <= 422)
                        {
                            point = 103 + ((spPoint - 412) / 10);
                        }
                        else if (spPoint <= 425)
                        {
                            point = 104 + ((spPoint - 415) / 10);
                        }
                        else if (spPoint <= 429)
                        {
                            point = 105 + ((spPoint - 419) / 10);
                        }
                        else if (spPoint <= 433)
                        {
                            point = 106 + ((spPoint - 423) / 10);
                        }
                        else if (spPoint <= 437)
                        {
                            point = 107 + ((spPoint - 427) / 10);
                        }
                        else if (spPoint <= 441)
                        {
                            point = 108 + ((spPoint - 431) / 10);
                        }
                        else if (spPoint <= 445)
                        {
                            point = 109 + ((spPoint - 435) / 10);
                        }
                        else if (spPoint <= 449)
                        {
                            point = 110 + ((spPoint - 439) / 10);
                        }
                        else if (spPoint <= 453)
                        {
                            point = 111 + ((spPoint - 443) / 10);
                        }
                        else if (spPoint <= 457)
                        {
                            point = 112 + ((spPoint - 447) / 10);
                        }
                        else if (spPoint <= 461)
                        {
                            point = 113 + ((spPoint - 451) / 10);
                        }
                        else if (spPoint <= 465)
                        {
                            point = 114 + ((spPoint - 455) / 10);
                        }
                        else if (spPoint <= 469)
                        {
                            point = 115 + ((spPoint - 459) / 10);
                        }
                        else if (spPoint <= 473)
                        {
                            point = 116 + ((spPoint - 463) / 10);
                        }
                        else if (spPoint <= 477)
                        {
                            point = 117 + ((spPoint - 467) / 10);
                        }
                        else if (spPoint <= 481)
                        {
                            point = 118 + ((spPoint - 471) / 10);
                        }
                        else if (spPoint <= 486)
                        {
                            point = 119 + ((spPoint - 476) / 10);
                        }

                        break;
                }
                return point;
            }
            catch
            {
                return 0;
            }
        }

        public static int SPPoint(short spLevel, short upgrade)
        {
            int point = (spLevel - 20) * 3;
            if (spLevel <= 20)
            {
                point = 0;
            }
            switch (upgrade)
            {
                case 1:
                    point += 5;
                    break;

                case 2:
                    point += 10;
                    break;

                case 3:
                    point += 15;
                    break;

                case 4:
                    point += 20;
                    break;

                case 5:
                    point += 28;
                    break;

                case 6:
                    point += 36;
                    break;

                case 7:
                    point += 46;
                    break;

                case 8:
                    point += 56;
                    break;

                case 9:
                    point += 68;
                    break;

                case 10:
                    point += 80;
                    break;

                case 11:
                    point += 95;
                    break;

                case 12:
                    point += 110;
                    break;

                case 13:
                    point += 128;
                    break;

                case 14:
                    point += 148;
                    break;

                case 15:
                    point += 173;
                    break;
            }

            if (upgrade > 15)
            {
                point += 173 + (25 + (5 * (upgrade - 15)));
            }

            return point;
        }

        internal static int Defence(ClassType @class, byte level)
        {
            if (_hitDef == null)
            {
                new CharacterHelper();
            }
            return _hitDef[(int)@class, level]; ;
        }

        internal static int DefenceRate(ClassType @class, byte level)
        {
            if (_hitDodge == null)
            {
                new CharacterHelper();
            }
            return _hitDodge[(int)@class, level]; ;
        }

        internal static int DistanceDefence(ClassType @class, byte level)
        {
            if (_distDef == null)
            {
                new CharacterHelper();
            }
            return _distDef[(int)@class, level]; ;
        }

        internal static int DistanceDefenceRate(ClassType @class, byte level)
        {
            if (_distDodge == null)
            {
                new CharacterHelper();
            }
            return _distDodge[(int)@class, level]; ;
        }

        internal static int DistanceRate(ClassType @class, byte level)
        {
            if (_distRate == null)
            {
                new CharacterHelper();
            }
            return _distRate[(int)@class, level]; ;
        }

        internal static int DistCritical(ClassType @class, byte level)
        {
            if (_criticalDist == null)
            {
                new CharacterHelper();
            }
            return _criticalDist[(int)@class, level]; ;
        }

        internal static int DistCriticalRate(ClassType @class, byte level)
        {
            if (_criticalDistRate == null)
            {
                new CharacterHelper();
            }
            return _criticalDistRate[(int)@class, level]; ;
        }

        internal static int HitCritical(ClassType @class, byte level)
        {
            if (_criticalHit == null)
            {
                new CharacterHelper();
            }
            return _criticalHit[(int)@class, level]; ;
        }

        internal static int HitCriticalRate(ClassType @class, byte level)
        {
            if (_criticalHitRate == null)
            {
                new CharacterHelper();
            }
            return _criticalHitRate[(int)@class, level]; ;
        }

        internal static int HitRate(ClassType @class, byte level)
        {
            if (_hitRate == null)
            {
                new CharacterHelper();
            }
            return _hitRate[(int)@class, level]; ;
        }

        // TODO: Improve with Official Source Code
        private static void loadStats()
        {
            _minHit = new int[5, 256];
            _maxHit = new int[5, 256];
            _hitRate = new int[5, 256];
            _criticalHitRate = new int[5, 256];
            _criticalHit = new int[5, 256];
            _minDist = new int[5, 256];
            _maxDist = new int[5, 256];
            _distRate = new int[5, 256];
            _criticalDistRate = new int[5, 256];
            _criticalDist = new int[5, 256];
            _hitDef = new int[5, 256];
            _hitDodge = new int[5, 256];
            _distDef = new int[5, 256];
            _distDodge = new int[5, 256];
            _magicalDef = new int[5, 256];

            for (int i = 0; i < 256; i++)
            {
                // ADVENTURER
                _minHit[(int)ClassType.Adventurer, i] = i + 9; // approx
                _maxHit[(int)ClassType.Adventurer, i] = i + 9; // approx
                _hitRate[(int)ClassType.Adventurer, i] = i + 9; // approx
                _criticalHitRate[(int)ClassType.Adventurer, i] = 0; // sure
                _criticalHit[(int)ClassType.Adventurer, i] = 0; // sure
                _minDist[(int)ClassType.Adventurer, i] = i + 9; // approx
                _maxDist[(int)ClassType.Adventurer, i] = i + 9; // approx
                _distRate[(int)ClassType.Adventurer, i] = (i + 9) * 2; // approx
                _criticalDistRate[(int)ClassType.Adventurer, i] = 0; // sure
                _criticalDist[(int)ClassType.Adventurer, i] = 0; // sure
                _hitDef[(int)ClassType.Adventurer, i] = i + (9 / 2); // approx
                _hitDodge[(int)ClassType.Adventurer, i] = i + 9; // approx
                _distDef[(int)ClassType.Adventurer, i] = (i + 9) / 2; // approx
                _distDodge[(int)ClassType.Adventurer, i] = i + 9; // approx
                _magicalDef[(int)ClassType.Adventurer, i] = (i + 9) / 2; // approx

                // SWORDMAN
                _criticalHitRate[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalHit[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalDist[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalDistRate[(int)ClassType.Swordsman, i] = 0; // approx
                _minDist[(int)ClassType.Swordsman, i] = i + 12; // approx
                _maxDist[(int)ClassType.Swordsman, i] = i + 12; // approx
                _distRate[(int)ClassType.Swordsman, i] = 2 * (i + 12); // approx
                _hitDodge[(int)ClassType.Swordsman, i] = i + 12; // approx
                _distDodge[(int)ClassType.Swordsman, i] = i + 12; // approx
                _magicalDef[(int)ClassType.Swordsman, i] = (i + 9) / 2; // approx
                _hitRate[(int)ClassType.Swordsman, i] = i + 27; // approx
                _hitDef[(int)ClassType.Swordsman, i] = i + 2; // approx

                _minHit[(int)ClassType.Swordsman, i] = (2 * i) + 5; // approx Numbers n such that 10n+9 is prime.
                _maxHit[(int)ClassType.Swordsman, i] = (2 * i) + 5; // approx Numbers n such that 10n+9 is prime.
                _distDef[(int)ClassType.Swordsman, i] = i; // approx

                // MAGICIAN
                _hitRate[(int)ClassType.Magician, i] = 0; // sure
                _criticalHitRate[(int)ClassType.Magician, i] = 0; // sure
                _criticalHit[(int)ClassType.Magician, i] = 0; // sure
                _criticalDistRate[(int)ClassType.Magician, i] = 0; // sure
                _criticalDist[(int)ClassType.Magician, i] = 0; // sure

                _minDist[(int)ClassType.Magician, i] = 14 + i; // approx
                _maxDist[(int)ClassType.Magician, i] = 14 + i; // approx
                _distRate[(int)ClassType.Magician, i] = (14 + i) * 2; // approx
                _hitDef[(int)ClassType.Magician, i] = (i + 11) / 2; // approx
                _magicalDef[(int)ClassType.Magician, i] = i + 4; // approx
                _hitDodge[(int)ClassType.Magician, i] = 24 + i; // approx
                _distDodge[(int)ClassType.Magician, i] = 14 + i; // approx

                _minHit[(int)ClassType.Magician, i] = (2 * i) + 9; // approx Numbers n such that n^2 is of form x^ 2 + 40y ^ 2 with positive x,y.
                _maxHit[(int)ClassType.Magician, i] = (2 * i) + 9; // approx Numbers n such that n^2 is of form x^2+40y^2 with positive x,y.
                _distDef[(int)ClassType.Magician, i] = 20 + i; // approx

                // ARCHER
                _criticalHitRate[(int)ClassType.Archer, i] = 0; // sure
                _criticalHit[(int)ClassType.Archer, i] = 0; // sure
                _criticalDistRate[(int)ClassType.Archer, i] = 0; // sure
                _criticalDist[(int)ClassType.Archer, i] = 0; // sure

                _minHit[(int)ClassType.Archer, i] = 9 + (i * 3); // approx
                _maxHit[(int)ClassType.Archer, i] = 9 + (i * 3); // approx
                int add = i % 2 == 0 ? 2 : 4;
                _hitRate[(int)ClassType.Archer, 1] = 41;
                _hitRate[(int)ClassType.Archer, i] += add; // approx
                _minDist[(int)ClassType.Archer, i] = 2 * i; // approx
                _maxDist[(int)ClassType.Archer, i] = 2 * i; // approx

                _distRate[(int)ClassType.Archer, i] = 20 + (2 * i); // approx
                _hitDef[(int)ClassType.Archer, i] = i; // approx
                _magicalDef[(int)ClassType.Archer, i] = i + 2; // approx
                _hitDodge[(int)ClassType.Archer, i] = 41 + i; // approx
                _distDodge[(int)ClassType.Archer, i] = i + 2; // approx
                _distDef[(int)ClassType.Archer, i] = i; // approx


                // MARTIAL ARTIST
                _criticalHitRate[(int)ClassType.MartialArtist, i] = 0; // sure
                _criticalHit[(int)ClassType.MartialArtist, i] = 0; // sure
                _criticalDist[(int)ClassType.MartialArtist, i] = 0; // sure
                _criticalDistRate[(int)ClassType.MartialArtist, i] = 0; // sure
                _minDist[(int)ClassType.MartialArtist, i] = i + 53; // sure
                _maxDist[(int)ClassType.MartialArtist, i] = i + 53; // sure
                _distRate[(int)ClassType.MartialArtist, i] = 2 * (i + 33); // sure
                _hitDodge[(int)ClassType.MartialArtist, i] = i + 53; // sure
                _distDodge[(int)ClassType.MartialArtist, i] = i + 43; // sure
                _magicalDef[(int)ClassType.MartialArtist, i] = 53; // sure
                _hitRate[(int)ClassType.MartialArtist, i] = i + 33; // sure
                _hitDef[(int)ClassType.MartialArtist, i] = i - 16; // sure

                _minHit[(int)ClassType.MartialArtist, i] = i + 49; // sure
                _maxHit[(int)ClassType.MartialArtist, i] = i + 49; // sure
                _distDef[(int)ClassType.MartialArtist, i] = i - 14; // sure

            }
        }

        private static void loadHeroXpData()
        {
            // Load SpData
            _heroXpData = new double[256];
            _heroXpData[0] = 949560;
            for (var i = 1; i < _heroXpData.Length; i++) _heroXpData[i] = Convert.ToInt64(_heroXpData[i - 1] * 1.08);
        }

        internal static long HpMax(ClassType @class, byte level)
        {
            HpService hpService = new HpService();
            return hpService.GetHp(@class, level);
        }

        internal async Task<long> HpMaxAsync(ClassType @class, byte level)
        {
            HpService hpService = new HpService();
            return await hpService.GetHpAsync(@class, level);
        }

        internal static int HpRegen(ClassType @class)
        {
            return
                @class == ClassType.Archer ? 32 :
                @class == ClassType.Magician ? 20 :
                @class == ClassType.Swordsman ? 26 :
                25;
        }

        internal static int HpRegenSitting(ClassType @class)
        {
            return
                @class == ClassType.Archer ? 60 :
                @class == ClassType.Magician ? 30 :
                @class == ClassType.Swordsman ? 90 :
                30;
        }

        private static void loadJobXPData()
        {
            // Load JobData
            _firstJobXpData = new double[21];
            _secondjobxpData = new double[256];
            _firstJobXpData[0] = 2200;
            _secondjobxpData[0] = 17600;
            for (var i = 1; i < _firstJobXpData.Length; i++) _firstJobXpData[i] = _firstJobXpData[i - 1] + 700;

            for (var i = 1; i < _secondjobxpData.Length; i++)
            {
                var var2 = 400;
                if (i > 3) var2 = 4500;
                if (i > 40) var2 = 15000;
                _secondjobxpData[i] = _secondjobxpData[i - 1] + var2;
            }
        }

        internal static long MpMax(ClassType @class, byte level)
        {
            MpService mpService = new MpService();
            return mpService.GetMp(@class, level);
        }

        internal static int MpRegen(ClassType @class)
        {
            return
                 @class == ClassType.Archer ? 28 :
                 @class == ClassType.Magician ? 40 :
                 @class == ClassType.Swordsman ? 16 :
                 5;
        }

        internal static int MpRegenSitting(ClassType @class)
        {
            return
                 @class == ClassType.Archer ? 50 :
                 @class == ClassType.Magician ? 80 :
                 @class == ClassType.Swordsman ? 30 :
                 10;
        }

        private static void loadSpeedData()
        {
            _speedData = new byte[5];
            _speedData[(int)ClassType.Adventurer] = 11;
            _speedData[(int)ClassType.Swordsman] = 12;
            _speedData[(int)ClassType.Archer] = 13;
            _speedData[(int)ClassType.Magician] = 11;

            _speedData[(int)ClassType.MartialArtist] = 11;
        }

        private static void loadSPXPData()
        {
            // Load SpData
            _spxpData = new double[256];
            _spxpData[0] = 15000;
            _spxpData[19] = 218000;
            for (var i = 1; i < 19; i++) _spxpData[i] = _spxpData[i - 1] + 10000;
            for (var i = 20; i < _spxpData.Length; i++) _spxpData[i] = _spxpData[i - 1] + 6 * (3 * i * (i + 1) + 1);
        }

        private static void loadXPData()
        {
            // Load XpData
            _xpData = new double[256];
            var v = new double[256];
            double variable = 1;
            v[0] = 540;
            v[1] = 960;
            _xpData[0] = 300;
            for (var i = 2; i < v.Length; i++) v[i] = v[i - 1] + 420 + 120 * (i - 1);
            for (var i = 1; i < _xpData.Length; i++)
            {
                if (i < 79)
                {
                    switch (i)
                    {
                        case 14:
                            variable = 6 / 3d;
                            break;

                        case 39:
                            variable = 19 / 3d;
                            break;

                        case 59:
                            variable = 70 / 3d;
                            break;
                    }

                    _xpData[i] = Convert.ToInt64(_xpData[i - 1] + variable * v[i - 1]);
                }

                if (i >= 79)
                {
                    switch (i)
                    {
                        case 79:
                            variable = 5000;
                            break;

                        case 82:
                            variable = 9000;
                            break;

                        case 84:
                            variable = 13000;
                            break;
                    }

                    _xpData[i] = Convert.ToInt64(_xpData[i - 1] + variable * (i + 2) * (i + 2));
                }

                // Console.WriteLine($"LvL {i}: xpdata: {_xpData[i - 1]} v: {v[i - 1]}");
            }
        }

        #endregion
    }
}