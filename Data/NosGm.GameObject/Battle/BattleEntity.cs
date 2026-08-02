using NosGm.Configuration;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject._Event;
using NosGm.GameObject.Event;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.ItemThread;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using NosGm.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static NosGm.Domain.BCardType;

namespace NosGm.GameObject.Battle
{
    public class BattleEntity
    {
        #region Members

        public short[] CantAttackEntitiesList =
        {
            2004, 2020, 965, 966, 967, 968, 433, 238, 239, 240, 258, 259, 260, 433, 797, 798, 2013, 2016, 390,
            424 //Fragmentos de meteorito
            ,
            425 //Tumba grande
            ,
            426 //Tumba pequeña
            ,
            453 //Portal hacia el Reino de la Muerte
            ,
            465 //Almeja gigante
            ,
            861 //Tronco
            ,
            862 //Árbol floreciente
            ,
            863 //Estatua del duende
            ,
            864 //Pentagrama
            ,
            865 //Círculo de la oscuridad
            ,
            866 //Arbusto sagrado
            ,
            867 //Altar sagrado
            ,
            869 //Nido de fisgón
            ,
            880 //Trigo
            ,
            881 //Mineral de hierro
            ,
            892 //Pista rara
            ,
            893 //Fuente misteriosa
            ,
            894 //Fuente secreta
            ,
            895 //Nido de pájaro
            ,
            896 //Nido de oro
            ,
            897 //Fuente mágica
            ,
            898 //Fuente estrafalaria
            ,
            902 //Rama mágica
            ,
            905 //Cartel de personas buscadas
            ,
            906 //Estatua de Shinebone
            ,
            907 //Caseta de vigilancia destruida
            ,
            908 //Suelo blando
            ,
            909 //Suelo áspero
            ,
            910 //Piedra brillante
            ,
            911 //Escoba
            ,
            912 //Planta de arroz
            ,
            913 //Estatua del horror
            ,
            914 //Árbol frutal
            ,
            915 //Marcador de puntos
            ,
            916 //Piedra roja
            ,
            917 //Estatua destruida de Shinebone
            ,
            920 //Cartel indicador pequeño
            ,
            921 //Cartel indicador grande
            ,
            922 //Cristal de arco iris grande
            ,
            923 //Cristal de arco iris mediano
            ,
            924 //Cristal de arco iris pequeño
            ,
            928 //Margarita
            ,
            929 //Lirio
            ,
            930 //Portal time-space
            ,
            931 //Nota de las hadas
            ,
            932 //Gema del alma amarilla
            ,
            933 //Gema del alma roja
            ,
            934 //Gema del alma azul
            ,
            938 //Estatua de Crhysos
            ,
            941 //Flor grande roja
            ,
            944 //Fuente
            ,
            953 //Gema del alma misteriosa
            ,
            954 //Gema del alma gris
            ,
            955 //Torre de teletransporte
            ,
            956 //Hoguera pequeña
            ,
            957 //Hoguera grande
            ,
            959 //Máquina de helado
            ,
            985 //Saco de Pascua
            ,
            988 //Huevo misterioso
            ,
            1264 //Saco con higos chumbos
            ,
            1265 //Buzón de Neil
            ,
            1266 //Humad prisionero
            ,
            1271 //Cofre de los comerciantes de Akamur abandonado
            ,
            1272 //Mercancía destruida
            ,
            1273 //Mercancía saqueada
            ,
            1274 //Bandera de Akamur destruida
            ,
            1275 //Primera mercancía
            ,
            1276 //Segunda mercancía
            ,
            1277 //Tercera mercancía
            ,
            1278 //Cuarta mercancía
            ,
            1279 //Quinta mercancía
            ,
            1280 //Sexta mercancía
            ,
            1281 //Cofre con lista de órdenes
            ,
            1282 //Poción sospechosa
            ,
            1283 //Baúl extraño de Keru
            ,
            1284 //Baúl extraño de Garton
            ,
            1285 //Reliquia robada de Keru
            ,
            1286 //Antigüedades robadas de Garton
            ,
            1287 //Reliquias robadas
            ,
            1288 //Baúl con pergaminos
            ,
            1289 //Fragmento de reliquia extraño
            ,
            1308 //Reliquias rotas
            ,
            1309 //Reliquia destruida
            ,
            1310 //Baúl sellado
            ,
            1311 //Fragmentos de reliquia rotos
            ,
            1312 //Fragmentos de reliquia rotos
            ,
            1313 //Fragmentos de reliquia rotos
            ,
            1314 //Fragmentos de reliquia rotos
            ,
            1315 //Primera lápida
            ,
            1316 //Segunda lápida
            ,
            1317 //Tercera lápida
            ,
            1318 //Lápida rota
            ,
            1319 //Lápida rota
            ,
            1320 //Lápida rota
            ,
            1321 //Lápida rota
            ,
            1322 //Monje muerto
            ,
            1323 //Clérigo muerto
            ,
            1327 //Baúl con polvo de hadas
            ,
            1328 //Carta de los ladrones del desierto
            ,
            1329 //Diario del viajero
            ,
            1330 //Botellita de veneno
            ,
            1331 //Bolsa de piel de un hada
            ,
            1332 //Polvo de hadas de la tierra
            ,
            1333 //Polvo de hadas del viento
            ,
            1334 //Polvo de hadas de la arena
            ,
            1335 //Frasquito de medicina
            ,
            1337 //Baúl de los comerciantes de Akamur
            ,
            1338 //Baúl perdido (campamento de Kerus)
            ,
            1339 //Baúl perdido (campamento de Garton)
            ,
            1340 //Mercancía perdida (Campamento de los Ladrones del Desierto)
            ,
            1341 //Cofre Perdido (Centro de los Ladrones)
            ,
            1342 //Baúl extraño sin descubrir
            ,
            1346 //Baúl de la banda de ladrones
            ,
            1385 //Señal de Halloween
            ,
            2004 //Flor de hielo
            ,
            2320 //Cuerda
            ,
            2321 //Hermanos temerosos
            ,
            2350 //Planta venenosa de la condenación
            ,
            4280, 4281, 4282
        };

        public short[] CantAttackToEntitiesList =
        {
            848, 849, 850, 851, 852, 854, 958, 2591, 2592, 2004, 2020, 425 //Tumba grande
            ,
            426 //Tumba pequeña
            ,
            453 //Portal hacia el Reino de la Muerte
            ,
            465 //Almeja gigante
            ,
            861 //Tronco
            ,
            862 //Árbol floreciente
            ,
            863 //Estatua del duende
            ,
            864 //Pentagrama
            ,
            865 //Círculo de la oscuridad
            ,
            866 //Arbusto sagrado
            ,
            867 //Altar sagrado
            ,
            869 //Nido de fisgón
            ,
            880 //Trigo
            ,
            881 //Mineral de hierro
            ,
            892 //Pista rara
            ,
            893 //Fuente misteriosa
            ,
            894 //Fuente secreta
            ,
            895 //Nido de pájaro
            ,
            896 //Nido de oro
            ,
            897 //Fuente mágica
            ,
            898 //Fuente estrafalaria
            ,
            902 //Rama mágica
            ,
            905 //Cartel de personas buscadas
            ,
            906 //Estatua de Shinebone
            ,
            907 //Caseta de vigilancia destruida
            ,
            908 //Suelo blando
            ,
            909 //Suelo áspero
            ,
            910 //Piedra brillante
            ,
            911 //Escoba
            ,
            912 //Planta de arroz
            ,
            913 //Estatua del horror
            ,
            914 //Árbol frutal
            ,
            915 //Marcador de puntos
            ,
            916 //Piedra roja
            ,
            917 //Estatua destruida de Shinebone
            ,
            920 //Cartel indicador pequeño
            ,
            921 //Cartel indicador grande
            ,
            922 //Cristal de arco iris grande
            ,
            923 //Cristal de arco iris mediano
            ,
            924 //Cristal de arco iris pequeño
            ,
            928 //Margarita
            ,
            929 //Lirio
            ,
            930 //Portal time-space
            ,
            931 //Nota de las hadas
            ,
            932 //Gema del alma amarilla
            ,
            933 //Gema del alma roja
            ,
            934 //Gema del alma azul
            ,
            938 //Estatua de Crhysos
            ,
            941 //Flor grande roja
            ,
            944 //Fuente
            ,
            953 //Gema del alma misteriosa
            ,
            954 //Gema del alma gris
            ,
            955 //Torre de teletransporte
            ,
            956 //Hoguera pequeña
            ,
            957 //Hoguera grande
            ,
            959 //Máquina de helado
            ,
            985 //Saco de Pascua
            ,
            988 //Huevo misterioso
            ,
            1264 //Saco con higos chumbos
            ,
            1265 //Buzón de Neil
            ,
            1266 //Humad prisionero
            ,
            1271 //Cofre de los comerciantes de Akamur abandonado
            ,
            1272 //Mercancía destruida
            ,
            1273 //Mercancía saqueada
            ,
            1274 //Bandera de Akamur destruida
            ,
            1275 //Primera mercancía
            ,
            1276 //Segunda mercancía
            ,
            1277 //Tercera mercancía
            ,
            1278 //Cuarta mercancía
            ,
            1279 //Quinta mercancía
            ,
            1280 //Sexta mercancía
            ,
            1281 //Cofre con lista de órdenes
            ,
            1282 //Poción sospechosa
            ,
            1283 //Baúl extraño de Keru
            ,
            1284 //Baúl extraño de Garton
            ,
            1285 //Reliquia robada de Keru
            ,
            1286 //Antigüedades robadas de Garton
            ,
            1287 //Reliquias robadas
            ,
            1288 //Baúl con pergaminos
            ,
            1289 //Fragmento de reliquia extraño
            ,
            1308 //Reliquias rotas
            ,
            1309 //Reliquia destruida
            ,
            1310 //Baúl sellado
            ,
            1311 //Fragmentos de reliquia rotos
            ,
            1312 //Fragmentos de reliquia rotos
            ,
            1313 //Fragmentos de reliquia rotos
            ,
            1314 //Fragmentos de reliquia rotos
            ,
            1315 //Primera lápida
            ,
            1316 //Segunda lápida
            ,
            1317 //Tercera lápida
            ,
            1318 //Lápida rota
            ,
            1319 //Lápida rota
            ,
            1320 //Lápida rota
            ,
            1321 //Lápida rota
            ,
            1322 //Monje muerto
            ,
            1323 //Clérigo muerto
            ,
            1327 //Baúl con polvo de hadas
            ,
            1328 //Carta de los ladrones del desierto
            ,
            1329 //Diario del viajero
            ,
            1330 //Botellita de veneno
            ,
            1331 //Bolsa de piel de un hada
            ,
            1332 //Polvo de hadas de la tierra
            ,
            1333 //Polvo de hadas del viento
            ,
            1334 //Polvo de hadas de la arena
            ,
            1335 //Frasquito de medicina
            ,
            1337 //Baúl de los comerciantes de Akamur
            ,
            1338 //Baúl perdido (campamento de Kerus)
            ,
            1339 //Baúl perdido (campamento de Garton)
            ,
            1340 //Mercancía perdida (Campamento de los Ladrones del Desierto)
            ,
            1341 //Cofre Perdido (Centro de los Ladrones)
            ,
            1342 //Baúl extraño sin descubrir
            ,
            1346 //Baúl de la banda de ladrones
            ,
            1385 //Señal de Halloween
            ,
            2004 //Flor de hielo
            ,
            2320 //Cuerda
            ,
            2321 //Hermanos temerosos
            ,
            2350 //Planta venenosa de la condenación
            ,
            1375, 1376, 1377, 1083, 1438, 1439, 856, 955, 853, 2018, 4280, 4281, 4282, 1436, 855, 860, 945,
            2352 // SP8M Meteorite
            ,
            2353 // SP8M Meteorite
            ,
            2329 // Cheese Chunk
            ,
            2330 // Grass Clump
        };

        #endregion

        #region Instantiation

        public BattleEntity(Character character, Skill skill)
        {
            Character = character;

            if (character.BattleEntity != null)
            {
                Buffs = character.Buff;
                BuffObservables = character.BuffObservables;
                BCardDisposables = character.BattleEntity.BCardDisposables;
                BCards = character.EquipmentBCards;
                CellonOptions = character.CellonOptions;
                OnDeathEvents = character.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>();
                CellonOptions = new ThreadSafeGenericList<CellonOptionDTO>();
                OnDeathEvents = new List<EventContainer>();
            }

            EntityType = EntityType.Player;
            UserType = UserType.Player;
            DamageMinimum = character.MinHit;
            DamageMaximum = character.MaxHit;
            Hitrate = character.HitRate;
            CritChance = character.HitCriticalChance;
            CritRate = character.HitCriticalRate;
            Morale = character.Level;
            FireResistance = character.FireResistance;
            WaterResistance = character.WaterResistance;
            LightResistance = character.LightResistance;
            ShadowResistance = character.DarkResistance;

            ItemInstance weapon = null;

            if (skill != null)
            {
                switch (skill.Type)
                {
                    case 0:
                        AttackType = AttackType.Melee;
                        if (character.Class == ClassType.Archer)
                        {
                            DamageMinimum = character.SecondWeaponMinHit;
                            DamageMaximum = character.SecondWeaponMaxHit;
                            Hitrate = character.SecondWeaponHitRate;
                            CritChance = character.SecondWeaponCriticalChance;
                            CritRate = character.SecondWeaponCriticalRate;
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon,
                                InventoryType.Wear);
                        }
                        else
                        {
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                                InventoryType.Wear);
                        }

                        break;

                    case 1:
                        AttackType = AttackType.Range;
                        if (character.Class == ClassType.Adventurer || character.Class == ClassType.Swordsman ||
                            character.Class == ClassType.Magician)
                        {
                            DamageMinimum = character.SecondWeaponMinHit;
                            DamageMaximum = character.SecondWeaponMaxHit;
                            Hitrate = character.SecondWeaponHitRate;
                            CritChance = character.SecondWeaponCriticalChance;
                            CritRate = character.SecondWeaponCriticalRate;
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon,
                                InventoryType.Wear);
                        }
                        else
                        {
                            weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                                InventoryType.Wear);
                        }

                        break;

                    case 2:
                        AttackType = AttackType.Magical;
                        weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                            InventoryType.Wear);
                        break;

                    case 3:
                        weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                            InventoryType.Wear);
                        switch (character.Class)
                        {
                            case ClassType.Adventurer:
                            case ClassType.Swordsman:
                                AttackType = AttackType.Melee;
                                break;

                            case ClassType.Archer:
                                AttackType = AttackType.Range;
                                break;

                            case ClassType.Magician:
                                AttackType = AttackType.Magical;
                                break;
                        }

                        break;

                    case 5:
                        AttackType = AttackType.Melee;
                        switch (character.Class)
                        {
                            case ClassType.Adventurer:
                            case ClassType.Swordsman:
                            case ClassType.Magician:
                                weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon,
                                    InventoryType.Wear);
                                break;

                            case ClassType.Archer:
                                DamageMinimum = character.SecondWeaponMinHit;
                                DamageMaximum = character.SecondWeaponMaxHit;
                                Hitrate = character.SecondWeaponHitRate;
                                CritChance = character.SecondWeaponCriticalChance;
                                CritRate = character.SecondWeaponCriticalRate;
                                weapon = character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon,
                                    InventoryType.Wear);
                                break;
                        }

                        break;
                }
            }
            else
            {
                weapon = character.Inventory?.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon,
                    InventoryType.Wear);
                switch (character.Class)
                {
                    case ClassType.Adventurer:
                    case ClassType.Swordsman:
                        AttackType = AttackType.Melee;
                        break;

                    case ClassType.Magician:
                    case ClassType.Archer:
                        AttackType = AttackType.Range;
                        break;
                }
            }

            if (weapon != null)
            {
                AttackUpgrade = weapon.Upgrade;
                WeaponDamageMinimum = weapon.DamageMinimum + weapon.Item.DamageMinimum;
                WeaponDamageMaximum = weapon.DamageMaximum + weapon.Item.DamageMaximum;

                if (weapon == character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear))
                {
                    ShellWeaponEffects = character.ShellEffectMain.ToList();
                    RuneWeaponEffects = character.RuneEffectMain.ToList();
                }
                else
                {
                    ShellWeaponEffects = character.ShellEffectSecondary.ToList();
                }
            }

            var fairy = character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (fairy != null)
            {
                FairyEnchantments = character.FairyEnchantments.ToList();
            }

            DamageMinimum -= WeaponDamageMinimum;
            DamageMaximum -= WeaponDamageMaximum;

            if (DamageMaximum <= DamageMinimum)
            {
                DamageMaximum = DamageMinimum + 1;
            }

            var armor = character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
            if (armor != null)
            {
                DefenseUpgrade = armor.Upgrade;
                ArmorMeleeDefense = armor.CloseDefence + armor.Item.CloseDefence;
                ArmorRangeDefense = armor.DistanceDefence + armor.Item.DistanceDefence;
                ArmorMagicalDefense = armor.MagicDefence + armor.Item.MagicDefence;

                ShellArmorEffects = character.ShellEffectArmor.ToList();
            }

            MeleeDefense = character.Defence - ArmorMeleeDefense;
            MeleeDefenseDodge = character.DefenceRate;
            RangeDefense = character.DistanceDefence - ArmorRangeDefense;
            RangeDefenseDodge = character.DistanceDefenceRate;
            MagicalDefense = character.MagicalDefence - ArmorMagicalDefense;
            Element = character.Element;

            ElementRate = character.ElementRate + character.ElementRateSP;
        }

        public BattleEntity(Mate mate)
        {
            Mate = mate;

            if (mate.BattleEntity != null)
            {
                Buffs = mate.Buff;
                BuffObservables = mate.BuffObservables;
                BCardDisposables = mate.BattleEntity.BCardDisposables;
                BCards = mate.BattleEntity.BCards;
                OnDeathEvents = mate.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>(mate.Monster.BCards);
                OnDeathEvents = new List<EventContainer>();
            }

            //Add Partner EquipmentBCards
            EntityType = EntityType.Mate;
            UserType = UserType.Npc;
            DamageMinimum = mate.DamageMinimum;
            DamageMaximum = mate.DamageMaximum;
            WeaponDamageMinimum = mate.WeaponInstance?.Item.DamageMinimum ?? 0;
            WeaponDamageMaximum = mate.WeaponInstance?.Item.DamageMaximum ?? 0;
            Hitrate = mate.Concentrate + (mate.WeaponInstance?.Item.HitRate ?? 0);
            CritChance = mate.Monster.CriticalChance + (mate.WeaponInstance?.Item.CriticalLuckRate ?? 0);
            CritRate = mate.Monster.CriticalRate + (mate.WeaponInstance?.Item.CriticalRate ?? 0);
            Morale = mate.Level;
            AttackUpgrade = mate.WeaponInstance?.Upgrade ?? mate.Attack;
            FireResistance = mate.Monster.FireResistance + (mate.GlovesInstance?.FireResistance ?? 0) +
                             (mate.GlovesInstance?.Item.FireResistance ?? 0) +
                             (mate.BootsInstance?.FireResistance ?? 0) + (mate.BootsInstance?.Item.FireResistance ?? 0);
            WaterResistance = mate.Monster.WaterResistance + (mate.GlovesInstance?.WaterResistance ?? 0) +
                              (mate.GlovesInstance?.Item.WaterResistance ?? 0) +
                              (mate.BootsInstance?.WaterResistance ?? 0) +
                              (mate.BootsInstance?.Item.WaterResistance ?? 0);
            LightResistance = mate.Monster.LightResistance + (mate.GlovesInstance?.LightResistance ?? 0) +
                              (mate.GlovesInstance?.Item.LightResistance ?? 0) +
                              (mate.BootsInstance?.LightResistance ?? 0) +
                              (mate.BootsInstance?.Item.LightResistance ?? 0);
            ShadowResistance = mate.Monster.DarkResistance + (mate.GlovesInstance?.DarkResistance ?? 0) +
                               (mate.GlovesInstance?.Item.DarkResistance ?? 0) +
                               (mate.BootsInstance?.DarkResistance ?? 0) +
                               (mate.BootsInstance?.Item.DarkResistance ?? 0);
            AttackType = (AttackType)mate.Monster.AttackClass;

            DefenseUpgrade = mate.ArmorInstance?.Upgrade ?? mate.Defence;
            MeleeDefense = mate.MeleeDefense;
            RangeDefense = mate.RangeDefense;
            MagicalDefense = mate.MagicalDefense;
            MeleeDefenseDodge = (mate.ArmorInstance?.Item.DefenceDodge ?? 0) + mate.MeleeDefenseDodge +
                                (mate.GlovesInstance?.Item.DefenceDodge ?? 0) +
                                (mate.BootsInstance?.Item.DefenceDodge ?? 0);
            RangeDefenseDodge = (mate.ArmorInstance?.Item.DistanceDefenceDodge ?? 0) + mate.RangeDefenseDodge +
                                (mate.GlovesInstance?.Item.DistanceDefenceDodge ?? 0) +
                                (mate.BootsInstance?.Item.DistanceDefenceDodge ?? 0);

            ArmorMeleeDefense = (mate.ArmorInstance?.Item.CloseDefence ?? 0) +
                                (mate.GlovesInstance?.Item.CloseDefence ?? 0) +
                                (mate.BootsInstance?.Item.CloseDefence ?? 0);
            ArmorRangeDefense = (mate.ArmorInstance?.Item.DistanceDefence ?? 0) +
                                (mate.GlovesInstance?.Item.DistanceDefence ?? 0) +
                                (mate.BootsInstance?.Item.DistanceDefence ?? 0);
            ArmorMagicalDefense = (mate.ArmorInstance?.Item.MagicDefence ?? 0) +
                                  (mate.GlovesInstance?.Item.MagicDefence ?? 0) +
                                  (mate.BootsInstance?.Item.MagicDefence ?? 0);

            Element = (byte)(mate.MateType == MateType.Pet ? mate.Monster.Element :
                mate.IsUsingSp ? mate.Sp.Instance.Item.Element : 0);
            ElementRate = mate.Monster.ElementRate;
        }

        public BattleEntity(MapMonster monster)
        {
            MapMonster = monster;

            if (monster.BattleEntity != null)
            {
                Buffs = monster.Buff;
                BuffObservables = monster.BuffObservables;
                BCardDisposables = monster.BattleEntity.BCardDisposables;
                OnDeathEvents = monster.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                OnDeathEvents = new List<EventContainer>();
            }

            BCards = new ThreadSafeGenericLockedList<BCard>(monster.Monster.BCards);

            EntityType = EntityType.Monster;
            UserType = UserType.Monster;
            if (monster.Owner?.Mate != null)
            {
                DamageMinimum = monster.Owner.Mate.DamageMinimum;
                DamageMaximum = monster.Owner.Mate.DamageMaximum;
            }
            else
            {
                DamageMinimum = monster.Monster.DamageMinimum;
                DamageMaximum = monster.Monster.DamageMaximum;
            }

            WeaponDamageMinimum = 0;
            WeaponDamageMaximum = 0;
            Hitrate = monster.Monster.Concentrate;
            CritChance = monster.Monster.CriticalChance;
            CritRate = monster.Monster.CriticalRate;
            Morale = monster.Monster.Level;
            AttackUpgrade = monster.Monster.AttackUpgrade;
            FireResistance = monster.Monster.FireResistance;
            WaterResistance = monster.Monster.WaterResistance;
            LightResistance = monster.Monster.LightResistance;
            ShadowResistance = monster.Monster.DarkResistance;
            AttackType = (AttackType)monster.Monster.AttackClass;
            DefenseUpgrade = monster.Monster.DefenceUpgrade;
            MeleeDefense = monster.Monster.CloseDefence;
            MeleeDefenseDodge = monster.Monster.DefenceDodge;
            RangeDefense = monster.Monster.DistanceDefence;
            RangeDefenseDodge = monster.Monster.DistanceDefenceDodge;
            MagicalDefense = monster.Monster.MagicDefence;
            ArmorMeleeDefense = 0;
            ArmorRangeDefense = 0;
            ArmorMagicalDefense = 0;
            Element = monster.Monster.Element;
            ElementRate = monster.Monster.ElementRate;
            Death = monster.Death;
        }

        public BattleEntity(MapNpc npc)
        {
            MapNpc = npc;

            if (npc.BattleEntity != null)
            {
                Buffs = npc.Buff;
                BuffObservables = npc.BuffObservables;
                BCardDisposables = npc.BattleEntity.BCardDisposables;
                BCards = new ThreadSafeGenericLockedList<BCard>(npc.Npc.BCards);
                OnDeathEvents = npc.OnDeathEvents;
            }
            else
            {
                Buffs = new ThreadSafeSortedList<short, Buff>();
                BuffObservables = new ThreadSafeSortedList<short, IDisposable>();
                BCardDisposables = new ThreadSafeSortedList<int, IDisposable>();
                BCards = new ThreadSafeGenericLockedList<BCard>();
                OnDeathEvents = new List<EventContainer>();
            }

            //npc.Buff.CopyTo(Buffs);
            EntityType = EntityType.Npc;
            UserType = UserType.Npc;
            DamageMinimum = 0;
            DamageMaximum = 0;
            WeaponDamageMinimum = npc.Npc.DamageMinimum;
            WeaponDamageMaximum = npc.Npc.DamageMaximum;
            Hitrate = npc.Npc.Concentrate;
            CritChance = npc.Npc.CriticalChance;
            CritRate = npc.Npc.CriticalRate;
            Morale = npc.Npc.Level;
            AttackUpgrade = npc.Npc.AttackUpgrade;
            FireResistance = npc.Npc.FireResistance;
            WaterResistance = npc.Npc.WaterResistance;
            LightResistance = npc.Npc.LightResistance;
            ShadowResistance = npc.Npc.DarkResistance;
            AttackType = (AttackType)npc.Npc.AttackClass;
            DefenseUpgrade = npc.Npc.DefenceUpgrade;
            MeleeDefense = npc.Npc.CloseDefence;
            MeleeDefenseDodge = npc.Npc.DefenceDodge;
            RangeDefense = npc.Npc.DistanceDefence;
            RangeDefenseDodge = npc.Npc.DistanceDefenceDodge;
            MagicalDefense = npc.Npc.MagicDefence;
            ArmorMeleeDefense = npc.Npc.CloseDefence;
            ArmorRangeDefense = npc.Npc.DistanceDefence;
            ArmorMagicalDefense = npc.Npc.MagicDefence;
            Element = npc.Npc.Element;
            ElementRate = npc.Npc.ElementRate;
        }

        #endregion

        #region Properties

        public EventEntity Event
        {
            get
            {
                if (Character != null)
                {
                    return Character.Event;
                }

                if (Mate != null)
                {
                    return Mate.Event;
                }

                if (MapMonster != null)
                {
                    return MapMonster.Event;
                }

                if (MapNpc != null)
                {
                    return MapNpc.Event;
                }

                return null;
            }
        }

        public double AdditionalHp { get; set; }

        public double AdditionalMp { get; set; }

        public int ArmorDefense { get; set; }

        public int ArmorMagicalDefense { get; }

        public int ArmorMeleeDefense { get; }

        public int ArmorRangeDefense { get; }

        public AttackType AttackType { get; }

        public short AttackUpgrade { get; set; }

        public ThreadSafeSortedList<int, IDisposable> BCardDisposables { get; set; }

        public ThreadSafeGenericLockedList<BCard> BCards { get; }

        public GridPos[][] BrushFireJagged
        {
            get
            {
                if (Character != null)
                {
                    return Character.BrushFireJagged;
                }

                if (Mate != null)
                {
                    return Mate.BrushFireJagged;
                }

                if (MapMonster != null)
                {
                    return MapMonster.BrushFireJagged;
                }

                if (MapNpc != null)
                {
                    return MapNpc.BrushFireJagged;
                }

                return null;
            }
            set
            {
                if (Character != null)
                {
                    Character.BrushFireJagged = value;
                }
                else if (Mate != null)
                {
                    Mate.BrushFireJagged = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.BrushFireJagged = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.BrushFireJagged = value;
                }
            }
        }

        public ThreadSafeSortedList<short, IDisposable> BuffObservables { get; set; }

        public ThreadSafeSortedList<short, Buff> Buffs { get; set; }

        public bool CanBeTargetted =>
            MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance ||
            TargettedByMonstersList(false).Count < MaxTargetedByMonstersCount(false) &&
            TargettedByMonstersList(true).Count < MaxTargetedByMonstersCount(true);

        public ThreadSafeGenericList<CellonOptionDTO> CellonOptions { get; set; }

        public Character Character { get; set; }

        public int CritChance { get; set; }

        public int CritRate { get; set; }

        public int DamageMaximum { get; set; }

        public int DamageMinimum { get; }

        public DateTime Death { get; set; }

        public int Defense { get; set; }

        public short DefenseUpgrade { get; set; }

        public int Dodge { get; set; }

        public short Element { get; }

        public int ElementRate { get; }

        public EntityType EntityType { get; }

        public long FalconFocusedEntityId { get; set; }

        public int FireResistance { get; set; }

        public bool HasEntity => Character != null || Mate != null || MapMonster != null || MapNpc != null;

        public int Hitrate { get; }

        public int Hp
        {
            get
            {
                if (Character != null)
                {
                    return Character.Hp;
                }

                if (Mate != null)
                {
                    return (int)Mate.Hp;
                }

                if (MapMonster != null)
                {
                    return (int)MapMonster.CurrentHp;
                }

                if (MapNpc != null)
                {
                    return (int)MapNpc.CurrentHp;
                }

                return 0;
            }
            set
            {
                if (Character != null)
                {
                    Character.Hp = value;
                }
                else if (Mate != null)
                {
                    Mate.Hp = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.CurrentHp = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.CurrentHp = value;
                }
            }
        }

        public int HpMax
        {
            get
            {
                if (Character != null)
                {
                    return (int)Character.HPLoad();
                }

                if (Mate != null)
                {
                    return (int)Mate.MaxHp;
                }

                if (MapMonster != null)
                {
                    return (int)MapMonster.MaxHp;
                }

                if (MapNpc != null)
                {
                    return (int)MapNpc.MaxHp;
                }

                return 0;
            }
        }

        public bool Invincible { get; set; }

        public DateTime LastDefence
        {
            get
            {
                if (Character != null)
                {
                    return Character.LastDefence;
                }

                if (Mate != null)
                {
                    return Mate.LastDefence;
                }

                if (MapMonster != null)
                {
                    return MapMonster.LastDefence;
                }

                if (MapNpc != null)
                {
                    return MapNpc.LastDefence;
                }

                return new DateTime();
            }
            set
            {
                if (Character != null)
                {
                    Character.LastDefence = value;
                }
                else if (Mate != null)
                {
                    Mate.LastDefence = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.LastDefence = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.LastDefence = value;
                }
            }
        }

        public DateTime LastMonsterAggro
        {
            get
            {
                if (Character != null)
                {
                    return Character.LastMonsterAggro;
                }

                if (Mate != null)
                {
                    return Mate.LastMonsterAggro;
                }

                if (MapMonster != null)
                {
                    return MapMonster.LastMonsterAggro;
                }

                if (MapNpc != null)
                {
                    return MapNpc.LastMonsterAggro;
                }

                return new DateTime();
            }
            set
            {
                if (Character != null)
                {
                    Character.LastMonsterAggro = value;
                }
                else if (Mate != null)
                {
                    Mate.LastMonsterAggro = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.LastMonsterAggro = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.LastMonsterAggro = value;
                }
            }
        }

        public int Level => Character?.Level ?? Mate?.Level ?? MapMonster?.Monster?.Level ?? MapNpc?.Npc?.Level ?? 0;

        public int LightResistance { get; set; }

        public int MagicalDefense { get; }

        public long MapEntityId
        {
            get
            {
                if (Character != null)
                {
                    return Character.CharacterId;
                }

                if (Mate != null)
                {
                    return Mate.MateTransportId;
                }

                if (MapMonster != null)
                {
                    return MapMonster.MapMonsterId;
                }

                if (MapNpc != null)
                {
                    return MapNpc.MapNpcId;
                }

                return 0;
            }
        }

        public MapInstance MapInstance => Character?.MapInstance ??
                                          Mate?.Owner?.MapInstance ?? MapMonster?.MapInstance ?? MapNpc?.MapInstance;

        public MapMonster MapMonster { get; set; }

        public MapNpc MapNpc { get; set; }

        public Mate Mate { get; set; }

        public int MeleeDefense { get; }

        public int MeleeDefenseDodge { get; }

        public int Morale { get; set; }

        public int Mp
        {
            get
            {
                if (Character != null)
                {
                    return Character.Mp;
                }

                if (Mate != null)
                {
                    return (int)Mate.Mp;
                }

                if (MapMonster != null)
                {
                    return (int)MapMonster.CurrentMp;
                }

                if (MapNpc != null)
                {
                    return (int)MapNpc.CurrentMp;
                }

                return 0;
            }
            set
            {
                if (Character != null)
                {
                    Character.Mp = value;
                }
                else if (Mate != null)
                {
                    Mate.Mp = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.CurrentMp = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.CurrentMp = value;
                }
            }
        }

        public int MpMax
        {
            get
            {
                if (Character != null)
                {
                    return (int)Character.MPLoad();
                }

                if (Mate != null)
                {
                    return (int)Mate.MaxMp;
                }

                if (MapMonster != null)
                {
                    return (int)MapMonster.MaxMp;
                }

                if (MapNpc != null)
                {
                    return (int)MapNpc.MaxMp;
                }

                return 0;
            }
        }

        public List<EventContainer> OnDeathEvents { get; set; }

        public short PositionX
        {
            get
            {
                if (Character != null)
                {
                    return Character.PositionX;
                }

                if (Mate != null)
                {
                    return Mate.PositionX;
                }

                if (MapMonster != null)
                {
                    return MapMonster.MapX;
                }

                if (MapNpc != null)
                {
                    return MapNpc.MapX;
                }

                return 0;
            }
            set
            {
                if (Character != null)
                {
                    Character.PositionX = value;
                }
                else if (Mate != null)
                {
                    Mate.PositionX = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.MapX = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.MapX = value;
                }
            }
        }

        public short PositionY
        {
            get
            {
                if (Character != null)
                {
                    return Character.PositionY;
                }

                if (Mate != null)
                {
                    return Mate.PositionY;
                }

                if (MapMonster != null)
                {
                    return MapMonster.MapY;
                }

                if (MapNpc != null)
                {
                    return MapNpc.MapY;
                }

                return 0;
            }
            set
            {
                if (Character != null)
                {
                    Character.PositionY = value;
                }
                else if (Mate != null)
                {
                    Mate.PositionY = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.MapY = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.MapY = value;
                }
            }
        }

        public object PVELockObject
        {
            get
            {
                if (Character != null)
                {
                    return Character.PVELockObject;
                }

                if (Mate != null)
                {
                    return Mate.PVELockObject;
                }

                if (MapMonster != null)
                {
                    return MapMonster.PVELockObject;
                }

                if (MapNpc != null)
                {
                    return MapNpc.PVELockObject;
                }

                return 0;
            }
            set
            {
                if (Character != null)
                {
                    Character.PVELockObject = value;
                }
                else if (Mate != null)
                {
                    Mate.PVELockObject = value;
                }
                else if (MapMonster != null)
                {
                    MapMonster.PVELockObject = value;
                }
                else if (MapNpc != null)
                {
                    MapNpc.PVELockObject = value;
                }
            }
        }

        public int RangeDefense { get; }

        public int RangeDefenseDodge { get; }

        public int Resistance { get; set; }

        public int ResistForcedMovement => GetBuff(CardType.AbsorbedSpirit,
            (byte)AdditionalTypes.AbsorbedSpirit.ResistForcedMovement)[0];

        public List<RuneEffectDTO> RuneWeaponEffects { get; }

        public List<FairyEnchantmentDTO> FairyEnchantments { get; }

        public int ShadowResistance { get; set; }

        public List<ShellEffectDTO> ShellArmorEffects { get; }

        public List<ShellEffectDTO> ShellWeaponEffects { get; }

        public UserType UserType { get; }

        public int WaterResistance { get; set; }

        public int WeaponDamageMaximum { get; }

        public int WeaponDamageMinimum { get; }


        #endregion

        #region Methods

        public void AddBuff(Buff indicator, BattleEntity sender, bool noMessage = false, short x = 0, short y = 0,
            bool forced = false)
        {
            int GetShellArmorEffectValue(ShellArmorEffectType effectType)
            {
                return Character.ShellEffectArmor?.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ??
                       0;
            }
            if (indicator.Card != null)
            {
                indicator.Level = sender.MapMonster?.Owner?.Level ?? sender.Level;

                indicator.Sender = sender;

                if (Character != null && (indicator.Card.BuffType == BuffType.Bad && Character.HasGodMode ||
                                          Character.InvisibleGm)
                    || Mate != null && (indicator.Card.BuffType == BuffType.Bad && Mate.Owner.HasGodMode ||
                                        Mate.Owner.InvisibleGm))
                {
                    return;
                }

                if (Character != null && indicator.Card.BuffType == BuffType.Bad)
                {
                    int ReduceBadEffect = GetShellArmorEffectValue(ShellArmorEffectType.ReducedAllNegativeEffect);
                    int rdn = ServerManager.RandomNumber();
                    if (ReduceBadEffect != 0)
                    {
                        if (rdn <= ReduceBadEffect)
                        {
                            Character.Session.SendPacket(Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NEGATIVE_EFFECT"), indicator.Card.Name), 20));
                            return;
                        }

                    }
                }

                if (MapMonster != null &&
                    (MapMonster.IsBoss || ServerManager.Instance.BossVNums.Contains(MapMonster.MonsterVNum)))
                {
                    if (!forced && indicator.Card.BuffType == BuffType.Bad &&
                        (indicator.Card.BCards.Any(b =>
                                 b.Type == (byte)CardType.SpecialAttack &&
                                 b.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack)
                      || indicator.Card.BCards.Any(b =>
                                 b.Type == (byte)CardType.Move &&
                                 b.SubType == (byte)AdditionalTypes.Move.MovementImpossible)
                      || indicator.Card.BCards.Any(b =>
                                 b.Type == (byte)CardType.Move && b.SubType == (byte)AdditionalTypes.Move.SetMovement)
                      || indicator.Card.BCards.Any(b =>
                                 b.Type == (byte)CardType.Move &&
                                 b.SubType == (byte)AdditionalTypes.Move.MovementSpeedDecreased)
                      || indicator.Card.BCards.Any(b =>
                                 b.Type == (byte)CardType.Move &&
                                 b.SubType == (byte)AdditionalTypes.Move.MoveSpeedDecreased)))
                    {
                        return;
                    }
                }

                if (indicator.Card.BCards.Any(newbuff => Buffs.GetAllItems().Any(b => b.Card.BCards.Any(buff =>
                                                                                                buff.CardId != newbuff.CardId
                                                                                             && (buff.Type == (byte)CardType.MaxHPMP && buff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased &&
                                                                                                        (newbuff.Type == (byte)CardType.MaxHPMP || newbuff.Type == (byte)CardType.BearSpirit)
                                                                                                        || newbuff.Type == (byte)CardType.MaxHPMP && newbuff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased &&
                                                                                                            (buff.Type == (byte)CardType.MaxHPMP || buff.Type == (byte)CardType.BearSpirit)
                                                                                                        || buff.Type == (byte)CardType.MaxHPMP &&
                                                                                                             (buff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased || buff.SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP) &&
                                                                                                           (newbuff.Type == (byte)CardType.BearSpirit && newbuff.SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP) ||
                                                                                                           buff.Type == (byte)CardType.MaxHPMP &&
                                                                                                           (buff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased || buff.SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP) &&
                                                                                                           newbuff.Type == (byte)CardType.BearSpirit && newbuff.SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP
                                                                                                           || newbuff.Type == (byte)CardType.MaxHPMP &&
                                                                                                           (newbuff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased || newbuff.SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP) &&
                                                                                                           buff.Type == (byte)CardType.BearSpirit && buff.SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP ||
                                                                                                           newbuff.Type == (byte)CardType.MaxHPMP && (newbuff.SubType == (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased || newbuff.SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP) &&
                                                                                                           buff.Type == (byte)CardType.BearSpirit && buff.SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP || buff.Type == (byte)CardType.MaxHPMP && newbuff.Type == (byte)CardType.MaxHPMP &&
                                                                                                           buff.SubType == newbuff.SubType || buff.Type == (byte)CardType.BearSpirit && newbuff.Type == (byte)CardType.BearSpirit && buff.SubType == newbuff.SubType)))))
                {
                    return;
                }

                if (indicator.Card.BCards.Any(s =>
                                      s.Type == (byte)CardType.LotusSkills &&
                                      s.SubType.Equals((byte)AdditionalTypes.LotusSkills.ChangeMoonSkills)))
                {
                    RemoveBuff(697);
                }

                if (indicator.Card.BCards.Any(s =>
                                      s.Type == (byte)CardType.LotusSkills &&
                                      s.SubType.Equals((byte)AdditionalTypes.LotusSkills.ChangeLotusSkills)))
                {
                    RemoveBuff(690);
                }

                switch (indicator.Card.CardId)
                {
                    //No potion stacking
                    case 116:
                        RemoveBuff(4041);
                        break;
                    case 117:
                        RemoveBuff(4042);
                        break;
                    case 118:
                        RemoveBuff(4043);
                        break;
                    case 119:
                        RemoveBuff(4046);
                        break;

                    case 71: //iron skin - don't stack with tom's power
                        RemoveBuff(4049);
                        break;

                    //temporary fix --> bear buff overrides these buffs
                    case 155:
                        RemoveBuff(4051);
                        RemoveBuff(4052);
                        RemoveBuff(4053);
                        break;

                    // Fortune Bushtails buffs --> remove flower buffs
                    case 710:
                    case 711:
                        RemoveBuff(378);
                        RemoveBuff(379);
                        break;

                    //temporary fix --> family buffs shouldn't override monch buff
                    case 4051:
                    case 4052:
                    case 4053:
                        RemoveBuff(533);
                        break;


                    case 272:
                    case 273:
                    case 274:
                        if (Buffs.Any(s => new short[] { 272, 273, 274 }.Contains(s.Card.CardId)))
                        {
                            return;
                        }

                        break;

                    case 75:
                    case 28:
                    case 29:
                        if (Buffs.Any(s => s.Card.CardId == 153))
                        {
                            return;
                        }

                        break;

                    case 153:
                        RemoveBuff(28);
                        RemoveBuff(29);
                        RemoveBuff(75);
                        break;

                    case 475: // Devil's Blessing
                        {
                            if (MapInstance?.MapInstanceType == MapInstanceType.RaidInstance
                                && MapMonster?.MonsterVNum == 2326 /* Witch Laurena */)
                            {
                                MapInstance.InstanceBag.LaurenaRound++;

                                MapInstance.Broadcast(StaticPacketHelper.Say(3, MapEntityId, 1,
                                    Language.Instance.GetMessageFromKey("GET_OVER_HERE")));
                                MapInstance.Broadcast($"npc_req 3 {MapEntityId} 9685");

                                Observable.Timer(TimeSpan.FromSeconds(1))
                                    .Subscribe(observer =>
                                    {
                                        if (MapInstance != null)
                                        {
                                            MapInstance.Broadcast($"ca_t {MapEntityId} 2000");
                                            TeleportTo(new MapCell { X = 53, Y = 59 });
                                            MapInstance.Broadcast($"guri 11 3 {MapEntityId}");

                                            var monstersToSummon = new List<MonsterToSummon>();

                                            // Increase n by 1 every round -- max. 2
                                            var n = Math.Min(MapInstance.InstanceBag.LaurenaRound, 1);

                                            for (var i = 0; i < n; i++)
                                            {
                                                var spawnCell =
                                                    GetRandomMapCellInRange(20) ?? new MapCell
                                                    { X = PositionX, Y = PositionY };

                                                monstersToSummon.Add(new MonsterToSummon(2327, spawnCell, null, true)
                                                {
                                                    DeathEvents =
                                                    {
                                                    new EventContainer(MapInstance, EventActionType.REMOVELAURENABUFF,
                                                        this)
                                                    }
                                                });
                                            }

                                            EventHelper.Instance.RunEvent(new EventContainer(MapInstance,
                                                EventActionType.SPAWNMONSTERS, monstersToSummon));
                                        }
                                    });

                                Observable.Timer(TimeSpan.FromSeconds(3))
                                    .Subscribe(observer => { MapInstance?.Broadcast("npc_req -1 -1"); });
                            }
                        }
                        break;

                   
                    case 889:
                        RemoveBuff(890);
                        RemoveBuff(891);
                        break;

                    case 890:
                        RemoveBuff(889);
                        RemoveBuff(891);
                        break;

                    case 891:
                        RemoveBuff(889);
                        RemoveBuff(890);
                        break;

                    case 562:
                        RemoveBuff(567);
                        break;

                    case 563:
                        RemoveBuff(568);
                        break;

                    case 564:
                        RemoveBuff(562);
                        break;

                    case 565:
                        RemoveBuff(563);
                        break;

                    case 567:
                        RemoveBuff(562);
                        RemoveBuff(563);
                        RemoveBuff(564);
                        RemoveBuff(565);
                        RemoveBuff(568);
                        break;

                    case 568:
                        RemoveBuff(562);
                        RemoveBuff(563);
                        RemoveBuff(564);
                        RemoveBuff(565);
                        RemoveBuff(567);
                        break;

                    case 589:
                        sender?.AddBuff(new Buff(592, sender.Level), sender);
                        break;

                    case 601:
                        RemoveBuff(602);
                        RemoveBuff(603);
                        break;

                    case 602:
                        RemoveBuff(601);
                        RemoveBuff(603);
                        break;

                    case 603:
                        RemoveBuff(601);
                        RemoveBuff(602);
                        break;

                    case 608:
                        RemoveBuff(617); // Magic Spell
                        RemoveBuff(609); // Fire
                        RemoveBuff(610); // Ice
                        RemoveBuff(611); // Light
                        RemoveBuff(612); // No Element
                        RemoveBuff(613); // Dark
                        break;

                    case 617:
                        RemoveBuff(608); // Magical Fetters
                        break;

                    case 727:
                        if (Character == null)
                        {
                            break;
                        }

                        if (Buffs.Any(s => s.Card.CardId == 728))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(728, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 729))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(729, 0));
                        }

                        MapInstance.Broadcast(Character.GenerateBfePacket(727, 1000));
                        break;

                    case 728:
                        if (Character == null)
                        {
                            break;
                        }

                        if (Buffs.Any(s => s.Card.CardId == 727))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(727, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 729))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(729, 0));
                        }

                        MapInstance.Broadcast(Character.GenerateBfePacket(728, 1000));
                        break;

                    case 729:
                        if (Character == null)
                        {
                            break;
                        }

                        if (Buffs.Any(s => s.Card.CardId == 727))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(727, 0));
                        }

                        if (Buffs.Any(s => s.Card.CardId == 728))
                        {
                            MapInstance.Broadcast(Character.GenerateBfePacket(728, 0));
                        }

                        MapInstance.Broadcast(Character.GenerateBfePacket(729, 1000));
                        break;

                    case 4041:
                        RemoveBuff(116);
                        break;
                    case 4042:
                        RemoveBuff(117);
                        break;
                    case 4043:
                        RemoveBuff(118);
                        break;
                    case 4044:
                        RemoveBuff(119);
                        break;

                    case 850:
                        Character.IsFishing = true;
                        break;
                }

                indicator.Card.BCards.ForEach(b => BCardDisposables[b.BCardId]?.Dispose());
                if (Buffs[indicator.Card.CardId] is Buff oldBuff)
                {
                    Buffs.Remove(indicator.Card.CardId);
                }

                Buffs[indicator.Card.CardId] = indicator;

                var buffTime = 0;
                var amuletMaxDurability = 0;
                if (Character != null)
                {
                    switch (indicator.Card.CardId)
                    {
                        case 85 when indicator.Card.Duration == 0:
                            buffTime = ServerManager.RandomNumber(50, 350);
                            break;

                        case 559 when indicator.Card.Duration == 0:
                            buffTime = ServerManager.RandomNumber(250, 450);
                            break;
                        case 4369 when indicator.Card.Duration == 0:
                            buffTime = ServerManager.RandomNumber(150, 450);
                            break;

                        case 336 when indicator.Card.Duration == 0:
                            if (Character.VehicleItem != null)
                            {
                                buffTime = Character.VehicleItem.SpeedBoostDuration * 10;
                            }
                            else
                            {
                                buffTime = ServerManager.RandomNumber(30, 70);
                            }

                            break;

                        case 0:
                            buffTime = Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue;
                            break;

                        case 562:
                        case 563:
                            buffTime = 400;
                            break;

                        case 319:
                            buffTime = 600;
                            break;

                        //Imp hat
                        case 2154:
                        case 2155:
                        case 2156:
                        case 2157:
                        case 2158:
                        case 2159:
                        case 2160:
                            buffTime = ServerManager.RandomNumber(100, 200);
                            break;
                    }
                } // End of if (Character != null)

                indicator.RemainingTime = indicator.Card.Duration == 0 ? buffTime : indicator.Card.Duration;

                // Amulet remaining time
                if (Character != null && indicator.Card.CardId == 62)
                {
                    var amulet =
                        Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                    if (amulet?.ItemDeleteTime != null)
                    {
                        buffTime = (int)amulet.ItemDeleteTime.Value.Subtract(DateTime.Now).TotalSeconds * 10;
                        indicator.RemainingTime = buffTime;
                    }
                    else if (amulet?.DurabilityPoint > 0)
                    {
                        amuletMaxDurability = amulet.Item.EffectValue;
                        buffTime = amulet.DurabilityPoint;
                        indicator.RemainingTime = buffTime;
                    }
                }

                indicator.Start = DateTime.Now;

                if (Character != null)
                {
                    Character.Session.SendPacket(
                        $"bf 1 {MapEntityId} {(indicator.Card.CardId == 0 ? Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue : amuletMaxDurability > 0 ? buffTime : 0)}.{indicator.Card.CardId}.{(indicator.Card.Duration == 0 || indicator.Card.CardId == 62 ? amuletMaxDurability > 0 ? amuletMaxDurability : buffTime : indicator.Card.Duration)} {sender.Level}");

                    if (!noMessage || !Buffs.Any(s => s.Card.CardId == indicator.Card.CardId))
                    {
                        Character.Session.SendPacket(Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("UNDER_EFFECT"), indicator.Card.Name),
                                20));
                    }

                    Character.Session.SendPacket(Character.GenerateStat());
                }

                if (Mate != null)
                {
                    Mate.Owner?.Session?.SendPackets(Mate.Owner.GeneratePst());
                }

                if (BuffObservables.ContainsKey(indicator.Card.CardId))
                {
                    BuffObservables[indicator.Card.CardId]?.Dispose();
                    BuffObservables.Remove(indicator.Card.CardId);
                }

                indicator.Card.BCards.ForEach(c => c.ApplyBCards(this, sender, x, y));

                if (indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.Move &&
                    !s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible)))
                {
                    if (Character != null)
                    {
                        Character.LoadSpeed();
                        Character.Session.SendPacket(Character.GenerateCond());
                        Character.LastSpeedChange = DateTime.Now;
                    }
                    else if (Mate != null)
                    {
                        Mate.Owner.Session.SendPacket(Mate.GenerateCond());
                    }
                }

                if (indicator.Card.BCards.Any(s
                    => s.Type == (byte)CardType.SpecialAttack &&
                       s.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack
                       || s.Type == (byte)CardType.Move && s.SubType == (byte)AdditionalTypes.Move.MovementImpossible
                       || s.Type == (byte)CardType.FrozenDebuff &&
                       s.SubType == (byte)AdditionalTypes.FrozenDebuff.EternalIce
                ))
                {
                    if (Character != null)
                    {
                        Character.Session.SendPacket(Character.GenerateCond());
                    }

                    if (Mate != null)
                    {
                        Mate.Owner.Session.SendPacket(Mate.GenerateCond());
                    }
                }

                if (indicator.Card.CardId == 518)
                {
                    MapInstance?.Broadcast($"eff {(byte)UserType} {MapEntityId} 4537");
                }
                MapInstance?.Broadcast($"bf_e {(short)UserType} {MapEntityId} {indicator.Card.CardId} 100");

                BuffObservables[indicator.Card.CardId] = Observable.Timer(TimeSpan.FromMilliseconds((indicator.Card.Duration == 0 || indicator.Card.CardId == 62 ? buffTime : indicator.Card.Duration) * 100)).Subscribe(o =>
                {
                    if (indicator.Card.CardId != 0 && amuletMaxDurability == 0)
                    {
                        RemoveBuff(indicator.Card.CardId);
                        if (indicator.Card.TimeoutBuff != 0 && ServerManager.RandomNumber() < indicator.Card.TimeoutBuffChance)
                        {
                            #region Do not add timeout buff if...  
                            if (indicator.Card.TimeoutBuff == 664) // Mega titan wings
                            {
                                if (Character != null && (Character.LastSkillUse.AddSeconds(10) <= DateTime.Now && Character.LastDefence.AddSeconds(10) <= DateTime.Now))
                                {

                                    return;
                                }
                            }
                            #endregion

                            AddBuff(new Buff(indicator.Card.TimeoutBuff, indicator.Level), sender);
                        }
                    }
                });
            }
        }

        public bool CanAttackEntity(BattleEntity receiver, bool isOwnerCheck = false, Skill skill = null)
        {
            if (receiver != null && this != receiver && (Hp > 0 || isOwnerCheck) && (receiver.Hp > 0 || isOwnerCheck))
            {
                if (EntityType == EntityType.Player && receiver.EntityType == EntityType.Player)
                {
                    bool isDedicatedPvpInstance =
                        MapInstance.MapInstanceType == MapInstanceType.ArenaInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.PVPInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.Act4Instance ||
                        MapInstance.MapInstanceType == MapInstanceType.CaligorInstance;

                    if (WorldPolicyConfiguration.IsPveWorld &&
                        (!WorldPolicyConfiguration.AllowDedicatedPvpInPveWorld || !isDedicatedPvpInstance))
                    {
                        return false;
                    }
                }

                if (MapMonster != null && CantAttackEntitiesList.Contains(MapMonster.MonsterVNum))
                {
                    return false;
                }

                if (MapNpc != null && CantAttackEntitiesList.Contains(MapNpc.NpcVNum))
                {
                    return false;
                }

                if (receiver.MapMonster != null && CantAttackToEntitiesList.Contains(receiver.MapMonster.MonsterVNum))
                {
                    return false;
                }

                if (receiver.MapNpc != null && CantAttackToEntitiesList.Contains(receiver.MapNpc.NpcVNum))
                {
                    return false;
                }

                if (MapInstance.InstanceBag?.EndState != 0)
                {
                    return false;
                }

                if (skill != null && skill.TargetType == 1)
                {
                    return true;
                }

                switch (EntityType)
                {
                    case EntityType.Player:
                        {
                            if (Character.Timespace != null && Character.Timespace.InstanceBag.EndState != 0)
                            {
                                return false;
                            }

                            //use same safezone pos for normal arena too
                            if (MapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId && (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 0
                                || receiver.MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 0))
                            {
                                return false;
                            }

                            // original safezone but disabled cuz i changed arenamap
                            //// User in SafeZone
                            //if (MapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId
                            //    && (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 0 &&
                            //        (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 16 ||
                            //         Character.PositionY == 35)
                            //        || receiver.MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value !=
                            //        0 && (MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 16 ||
                            //              receiver.PositionY == 35)))
                            //{
                            //    return false;
                            //}

                            if (MapInstance.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId && (MapInstance.Map.JaggedGrid[Character.PositionX][Character.PositionY]?.Value != 0
                                 || receiver.MapInstance.Map.JaggedGrid[receiver.PositionX][receiver.PositionY]?.Value != 0))


                            {
                                return false;
                            }

                            if (MapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance
                                && ((receiver.PositionX >= 30 && receiver.PositionX <= 34 && receiver.PositionY >= 73 && receiver.PositionY <= 77) || (receiver.PositionX >= 83 && receiver.PositionX <= 87 && receiver.PositionY >= 2 && receiver.PositionY <= 6)))
                            {
                                return false;
                            }

                            switch (receiver.EntityType)
                            {
                                case EntityType.Player:
                                    {
                                        if (receiver.Character.InvisibleGm)
                                        {
                                            return false;
                                        }

                                        if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                                        {
                                            if (Character.Faction != receiver.Character.Faction
                                                && MapInstance.Map.MapId != 130
                                                && MapInstance.Map.MapId != 131)
                                            {
                                                return true;
                                            }
                                        }
                                        else if (MapInstance.Map.MapTypes.Any(m =>
                                                m.MapTypeId == (short)MapTypeEnum.PVPMap) || MapInstance.IsPVP
                                                                                           || WorldPolicyConfiguration.IsPvpWorld &&
                                                                                              MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance &&
                                                                                              !WorldPolicyConfiguration.IsPvpSafeMap(MapInstance.Map.MapId)
                                                                                           || HasBuff(CardType.SpecialEffects,
                                                                                               (byte)AdditionalTypes
                                                                                                   .SpecialEffects
                                                                                                   .AbleToFightPVP) &&
                                                                                           receiver.HasBuff(
                                                                                               CardType.SpecialEffects,
                                                                                               (byte)AdditionalTypes
                                                                                                   .SpecialEffects
                                                                                                   .AbleToFightPVP))
                                        {
                                            if (MapInstance == ServerManager.Instance.FamilyArenaInstance
                                                && (Character.Family != null && receiver.Character.Family != null &&
                                                    Character.Family == receiver.Character.Family
                                                    || Character.Family == null && receiver.Character.Family == null))
                                            {
                                                return false;
                                            }

                                            ConcurrentBag<ArenaTeamMember> team = null;
                                            if (MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                            {
                                                team = ServerManager.Instance.ArenaTeams.ToList()
                                                                    .FirstOrDefault(s => s.Any(o => o.Session == Character.Session));
                                            }

                                            ConcurrentBag<ClientSession> iceteam = null;
                                            if (MapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                                            {
                                                if (IceBreaker.FrozenPlayers.Contains(receiver.Character.Session))
                                                {
                                                    return false;
                                                }

                                                if (IceBreaker.IceBreakerTeams.FirstOrDefault(
                                                            t => t.Contains(Character.Session)) !=
                                                    null)
                                                {
                                                    iceteam = IceBreaker.IceBreakerTeams.FirstOrDefault(t =>
                                                            t.Contains(Character.Session));
                                                }
                                            }

                                            if (team != null &&
                                                team.FirstOrDefault(s => s.Session == Character.Session)?.ArenaTeamType !=
                                                team.FirstOrDefault(s => s.Session == receiver.Character.Session)?.ArenaTeamType
                                                || MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance &&
                                                (Character.Group == null ||
                                                 !Character.Group.IsMemberOfGroup(receiver.Character.CharacterId))
                                                && (iceteam == null || !iceteam.Contains(receiver.Character.Session)))
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                    break;

                                case EntityType.Mate:
                                    {
                                        if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) &&
                                            !receiver.Mate.IsTemporalMate)
                                        {
                                            return false;
                                        }

                                        return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                    }
                                case EntityType.Monster:
                                    if (receiver.MapMonster.Owner == null || receiver.MapMonster.Owner.MapEntityId !=
                                                                          MapEntityId &&
                                                                          CanAttackEntity(receiver.MapMonster.Owner, true)
                                                                          || receiver.MapMonster.Owner.MapEntityId ==
                                                                          MapEntityId &&
                                                                          MapInstance == Character.Miniland &&
                                                                          IsMateTrainer(receiver.MapMonster.MonsterVNum))
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 ||
                                            receiver.MapMonster.Faction == FactionType.None ||
                                            receiver.MapMonster.Faction != Character.Faction)
                                        {
                                            if (receiver.MapMonster.IsDisabled ||
                                                !isOwnerCheck && receiver.MapMonster.IsJumping)
                                            {
                                                return false;
                                            }

                                            if (!IsMateTrainer(receiver.MapMonster.MonsterVNum) &&
                                                (receiver.MapMonster.Owner?.Character != null ||
                                                 receiver.MapMonster.Owner?.Mate != null))
                                            {
                                                return false;
                                            }

                                            return true;
                                        }
                                    }

                                    break;

                                case EntityType.Npc:
                                    if (receiver.MapNpc.IsDisabled)
                                    {
                                        return false;
                                    }

                                    if (receiver.MapNpc.NpcVNum == 1457)
                                    {
                                        return true;
                                    }

                                    break;
                            }
                        }
                        break;

                    case EntityType.Mate:
                        {
                            if ((Mate.Owner.IsVehicled || Mate.Owner.Invisible) && !Mate.IsTemporalMate)
                            {
                                return false;
                            }

                            return Mate.Owner.BattleEntity.CanAttackEntity(receiver, true);
                        }

                    case EntityType.Monster:
                        {
                            if (MapMonster.IsDisabled || !isOwnerCheck && MapMonster.IsJumping)
                            {
                                return false;
                            }

                            if (MapMonster.Owner != null)
                            {
                                if (IsMateTrainer(MapMonster.MonsterVNum))
                                {
                                    if (receiver.Mate != null &&
                                        receiver.Mate.Owner.CharacterId == MapMonster.Owner.MapEntityId &&
                                        MapInstance == receiver.Mate.Owner.Miniland)
                                    {
                                        return true;
                                    }

                                    if (receiver.MapMonster?.Owner != null && receiver.MapMonster.Owner == MapMonster.Owner)
                                    {
                                        return false;
                                    }
                                }

                                return MapMonster.Owner.CanAttackEntity(receiver, true);
                            }

                            switch (receiver.EntityType)
                            {
                                case EntityType.Player:
                                    {
                                        if (receiver.Character.Timespace != null &&
                                            receiver.Character.Timespace.InstanceBag.EndState != 0)
                                        {
                                            return false;
                                        }

                                        if ((ServerManager.Instance.ChannelId != 51 ||
                                             MapMonster.Faction == FactionType.None ||
                                             MapMonster.Faction != receiver.Character.Faction)
                                         && !receiver.Character.InvisibleGm)
                                        {
                                            return true;
                                        }
                                    }
                                    break;

                                case EntityType.Mate:
                                    {
                                        if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) &&
                                            !receiver.Mate.IsTemporalMate)
                                        {
                                            return false;
                                        }

                                        return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                    }
                                case EntityType.Monster:
                                    {
                                        if (receiver.MapMonster.IsDisabled || receiver.MapMonster.IsJumping)
                                        {
                                            return false;
                                        }

                                        if (receiver.MapMonster.Owner?.Mate != null)
                                        {
                                            return false;
                                        }

                                        return CanAttackEntity(receiver.MapMonster.Owner, true);
                                    }
                                case EntityType.Npc:
                                    {
                                        if (receiver.MapNpc.IsDisabled || receiver.MapNpc.Shop != null)
                                        {
                                            return false;
                                        }

                                        return true;
                                    }
                            }
                        }
                        break;

                    case EntityType.Npc:
                        {
                            if (MapNpc.IsDisabled || MapNpc.Shop != null)
                            {
                                return false;
                            }

                            /*if (MapNpc.Owner != null)
                            {
                                return MapNpc.Owner.BattleEntity.CanAttackEntity(receiver, true);
                            }
                            else*/
                            {
                                switch (receiver.EntityType)
                                {
                                    case EntityType.Player:
                                        return false;
                                    /*{
                                        if (!receiver.Character.InvisibleGm)
                                        {
                                            return true;
                                        }
                                    }
                                    break;*/
                                    case EntityType.Mate:
                                        {
                                            if ((receiver.Mate.Owner.IsVehicled || receiver.Mate.Owner.Invisible) &&
                                                !receiver.Mate.IsTemporalMate)
                                            {
                                                return false;
                                            }

                                            return CanAttackEntity(receiver.Mate.Owner.BattleEntity, true);
                                        }
                                    case EntityType.Monster:
                                        {
                                            if (receiver.MapMonster.IsDisabled || receiver.MapMonster.IsJumping)
                                            {
                                                return false;
                                            }

                                            if (receiver.MapMonster.Owner?.Mate != null)
                                            {
                                                return false;
                                            }

                                            if (receiver.MapMonster.Owner != null)
                                            {
                                                return CanAttackEntity(receiver.MapMonster.Owner, true);
                                            }

                                            return true;
                                        }
                                    case EntityType.Npc:
                                        {
                                            if (receiver.MapNpc.IsDisabled)
                                            {
                                                return false;
                                            }

                                            return true;
                                        }
                                }
                            }
                        }
                        break;
                }
            }

            return false;
        }

        public void ClearEnemyFalcon()
        {
            bool IsActivated = false;
            if (IsActivated)
            {
                MapInstance?.BattleEntities?.Where(s => s?.FalconFocusedEntityId == MapEntityId).ToList().ForEach(s =>
                {
                    s?.ClearOwnFalcon();
                });
            }
        }

        public void ClearOwnFalcon()
        {
            bool IsActivated = false;
            if (IsActivated)
            {
                if (FalconFocusedEntityId != 0)
                {
                    if (MapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == FalconFocusedEntityId) is
                        BattleEntity FalconFocusedEntity)
                    {
                        MapInstance.Broadcast(
                                $"eff_ob {(byte)FalconFocusedEntity.UserType} {FalconFocusedEntityId} 0 4269");
                    }

                    FalconFocusedEntityId = 0;
                }
            }
        }

        public void PushBackSession(int PushDistance, BattleEntity receiver, BattleEntity point)
        {
            if (receiver.MapMonster != null
            && (receiver.MapMonster.IsBoss
             || ServerManager.Instance.BossVNums.Contains(receiver.MapMonster.MonsterVNum)
             || receiver.MapMonster.Owner?.Character != null || receiver.MapMonster.Owner?.Mate != null))
            {
                return;
            }

            if (receiver.MapNpc != null
            && (ServerManager.Instance.BossVNums.Contains(receiver.MapNpc.NpcVNum)))
            {
                return;
            }

            receiver.Character?.WalkDisposable?.Dispose();

            short NewX = receiver.PositionX;
            short NewY = receiver.PositionY;

            if (point.PositionX == receiver.PositionX && point.PositionY == receiver.PositionY)
            {
                MapCell randomCell = receiver.MapInstance.Map.GetRandomPositionByDistance(point.PositionX, point.PositionY, (short)PushDistance);
                if (randomCell != null)
                {
                    NewX = randomCell.X;
                    NewY = randomCell.Y;
                }
            }
            else
            {
                Point3D pointPosition = new Point3D(point.PositionX, point.PositionY, 0.0);
                Point3D receiverPosition = new Point3D(receiver.PositionX, receiver.PositionY, 0.0);
                Point3D newPosition = new Point3D(receiver.PositionX, receiver.PositionY, 0.0);
                for (int i = 0; i == 0 || (PushDistance - i >= 0 && receiver.MapInstance.Map.isBlockedZone(receiver.PositionX, receiver.PositionY, (int)Math.Round(newPosition.X), (int)Math.Round(newPosition.Y))); i++)
                {
                    Vector3D v = receiverPosition - pointPosition;
                    double offset = PushDistance - i;
                    newPosition = ExtendLine(receiverPosition, offset, v);
                }
                NewX = (short)Math.Round(newPosition.X);
                NewY = (short)Math.Round(newPosition.Y);
            }

            receiver.PositionX = NewX;
            receiver.PositionY = NewY;

            if (receiver == this)
            {
                receiver.MapInstance.Broadcast($"guri 3 {(short)receiver.UserType} {receiver.MapEntityId} {receiver.PositionX} {receiver.PositionY} 3 {(int)(PushDistance / 4)} 2 -1");
            }
            else
            {
                receiver.MapInstance.Broadcast($"guri 3 {(short)receiver.UserType} {receiver.MapEntityId} {receiver.PositionX} {receiver.PositionY} 3 {PushDistance} 2 -1");
            }
        }

        private static Point3D ExtendLine(Point3D point, double offset, Vector3D vector)
        {
            vector.Normalize();
            double _offset = offset / vector.Length;
            Vector3D _vector = vector * _offset;
            Matrix3D m = new Matrix3D();
            m.Translate(_vector);
            Point3D result = m.Transform(point);
            return result;
        }

        public void ClearSacrificeBuff()
        {
            if (Buffs.FirstOrDefault(s => s.Card.BCards.Any(b =>
                    b.Type.Equals((byte)CardType.DamageConvertingSkill) &&
                    b.SubType.Equals((byte)AdditionalTypes.DamageConvertingSkill.TransferInflictedDamage)))
                ?.Sender is BattleEntity SacrificeSender)
            {
                SacrificeSender.RemoveBuff(546);
            }

            RemoveBuff(531);
        }

        public void IncreaseHp(int hp)
        {
            if (Hp + hp <= HpMax)
            {
                Hp += hp;
            }
            else
            {
                Hp = HpMax;
            }

            MapInstance.Broadcast(GenerateRc(hp));

            if (Character == null)
            {
                return;
            }

            Character.Session.SendPacket(Character.GenerateStat());
        }

        public void IncreaseMp(int mp)
        {
            if (Mp + mp <= MpMax)
            {
                Mp += mp;
            }
            else
            {
                Hp = MpMax;
            }

            if (Character == null)
            {
                return;
            }

            Character.Session.SendPacket(Character.GenerateStat());
        }

        public void DecreaseMp(int amount)
        {
            if (CellonOptions != null)
            {
                amount = (short)(amount * ((100 - CellonOptions.Where(s => s.Type == CellonOptionType.MPUsage)
                                                                .Sum(s => s.Value)) / 100D));
            }

            if (GetBuff(CardType.HealingBurningAndCasting,
                    (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int
                HPDecreasedByConsumingMP)
            {
                if (HPDecreasedByConsumingMP < 0)
                {
                    var amountDecreased = amount * HPDecreasedByConsumingMP / 100;
                    GetDamage(amountDecreased, this, true);
                    amount -= amountDecreased;
                }
            }

            if (Character != null)
            {
                if (Character.BattleEntity.AdditionalMp > amount)
                {
                    Character.BattleEntity.AdditionalMp -= amount;
                    amount = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
                else if (Character.BattleEntity.AdditionalMp > 0)
                {
                    amount -= (int)Character?.BattleEntity.AdditionalMp;
                    Character.BattleEntity.AdditionalMp = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
            }

            Mp -= amount;
            if (Mp < 0)
            {
                Mp = 0;
            }

            Character?.Session.SendPacket(Character.GenerateStat());
        }

        public void DisableBuffs(BuffType type, int level = 100)
        {
            if (type == BuffType.All || type == BuffType.Good)
            {
                ClearSacrificeBuff();
            }

            var BuffsCopy = new List<Buff>();

            lock (Buffs)
            {
                BuffsCopy = Buffs.GetAllItems();
            }

            var buff = BuffsCopy.Where(s =>
                (type == BuffType.All || s.Card.BuffType == type) && !s.StaticBuff && s.Card.Level < level &&
                s.Card.CardId != 62).ToList();

            buff.ForEach(s =>
            {
                s.Card.BCards.ForEach(b =>
                {
                    if (BCardDisposables?.ContainsKey(b.BCardId) == true && BCardDisposables[b.BCardId] != null)
                    {
                        BCardDisposables[b.BCardId]?.Dispose();
                        BCardDisposables[b.BCardId] = null;
                    }
                });

                if (BuffObservables != null && BuffObservables.ContainsKey(s.Card.CardId))
                {
                    BuffObservables[s.Card.CardId]?.Dispose();
                    BuffObservables.Remove(s.Card.CardId);
                }

                RemoveBuff(s.Card.CardId);
            });

            if (type == BuffType.All)
            {
                var StaticBuffsBCardDisposables = new ThreadSafeSortedList<int, IDisposable>();

                Buffs.Where(s => s.StaticBuff)
                    .SelectMany(s => s.Card.BCards)
                    .Where(s => BCardDisposables.ContainsKey(s.BCardId)).ToList()
                    .ForEach(s => StaticBuffsBCardDisposables[s.BCardId] = BCardDisposables[s.BCardId]);

                BCardDisposables.GetAllItems().Except(StaticBuffsBCardDisposables.GetAllItems()).ToList().ForEach(s =>
                {
                    s?.Dispose();
                });

                BCardDisposables = StaticBuffsBCardDisposables;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="types"></param>
        /// <param name="level"></param>
        public void DisableBuffs(List<BuffType> types, int level = 100)
        {
            types.ForEach(bt => DisableBuffs(bt, level));
        }

        public string GenerateDm(int dmg) => $"dm {(short)UserType} {MapEntityId} {dmg} 1";

        public string GenerateOut() => $"out {EntityType} {MapEntityId}";

        public string GenerateRc(int characterHealth) => $"rc {(short)UserType} {MapEntityId} {characterHealth} 0";

        public string GenerateTp()
        {
            Character?.WalkDisposable?.Dispose();
            return $"tp {(short)UserType} {MapEntityId} {PositionX} {PositionY} 0";
        }

        public int[] GetBuff(CardType type, byte subtype, int secondData = -1, int addValue = 0)
        {
            var value1 = 0;
            var value2 = 0;
            var value3 = 0;
            var value4 = 0;

            foreach (var entry in BCards.Where(s =>
                s?.Type.Equals((byte)type) == true && s.SubType.Equals(subtype) &&
                (secondData == -1 || s.SecondData == secondData)))
            {
                if (entry.IsLevelScaled)
                {
                    if (entry.IsLevelDivided)
                    {
                        value1 += Level / (entry.FirstData + addValue);
                    }
                    else
                    {
                        value1 += (entry.FirstData + addValue) * Level;
                    }
                }
                else
                {
                    value1 += entry.FirstData + addValue;
                }

                value2 += entry.SecondData;
                value3 += entry.ThirdData;
                value4++;
            }

            lock (Buffs)
            {
                foreach (var buff in Buffs.GetAllItems())
                    // THIS ONE DOES NOT FOR STUFFS

                    foreach (var entry in buff.Card.BCards
                        .Where(s => s.Type.Equals((byte)type) && s.SubType.Equals(subtype) &&
                                    (secondData == -1 || s.SecondData == secondData) &&
                                    (s.CastType != 1 || s.CastType == 1 &&
                                     buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now)))
                    {
                        if (entry.IsLevelScaled)
                        {
                            if (entry.IsLevelDivided)
                            {
                                value1 += buff.Level / (entry.FirstData + addValue);
                            }
                            else
                            {
                                value1 += (entry.FirstData + addValue) * buff.Level;
                            }
                        }
                        else
                        {
                            value1 += entry.FirstData + addValue;
                        }

                        value2 += entry.SecondData;
                        value3 += entry.ThirdData;
                        value4++;
                    }
            }

            if (Character != null && Character.Skills != null)
            {
                var PassiveSkillsBCards =
                    PassiveSkillHelper.Instance.PassiveSkillToBCards(
                        Character.Skills.Where(s => s.Skill.SkillType == 0));
                foreach (var entry in PassiveSkillsBCards.Where(s =>
                    s?.Type.Equals((byte)type) == true && s.SubType.Equals(subtype) &&
                    (secondData == -1 || s.SecondData == secondData)))
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += Level / (entry.FirstData + addValue);
                        }
                        else
                        {
                            value1 += (entry.FirstData + addValue) * Level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData + addValue;
                    }

                    value2 += entry.SecondData;
                    value3 += entry.ThirdData;
                    value4++;
                }

                foreach (var entry in Character.EffectFromTitle.Where(s =>
                    s?.Type.Equals((byte)type) == true && s.SubType.Equals(subtype) &&
                    (secondData == -1 || s.SecondData == secondData)))
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += Level / (entry.FirstData + addValue);
                        }
                        else
                        {
                            value1 += (entry.FirstData + addValue) * Level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData + addValue;
                    }

                    value2 += entry.SecondData;
                    value3 += entry.ThirdData;
                    value4++;
                }
            }

            return new[] { value1, value2, value3 };
        }

        public void HealEntity(int rawAmountToHeal, bool percentage = false)
        {
            var amountToHeal = 0;
            if (!percentage)
            {
                amountToHeal = (rawAmountToHeal + Hp) >= HpMax ? (HpMax - Hp) : rawAmountToHeal;
            }
            else
            {
                var percentageToHeal = (HpMax / 100) * rawAmountToHeal;
                amountToHeal = (percentageToHeal + Hp) >= HpMax ? (HpMax - Hp) : percentageToHeal;
            }

            // Who the hell knows
            if (MapInstance == null) return;

            MapInstance.Broadcast(GenerateRc(amountToHeal));
            Hp += amountToHeal;
        }

        public int GetDamage(int damage, BattleEntity damager, bool dontKill = false, bool fromDebuff = false)
        {
            if (Character?.HasGodMode == true || Mate?.Owner.HasGodMode == true || HasBuff(CardType.HideBarrelSkill,
                    (byte)AdditionalTypes.HideBarrelSkill.NoHPConsumption))
            {
                return 0;
            }

            if (fromDebuff)
            // If it comes from attack percent defense, dismin damage percent with chance, and static damages are already applied
            {
                var percentDefense = GetBuff(CardType.RecoveryAndDamagePercent,
                    (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP);
                if (percentDefense[0] != 0)
                {
                    var percentDefenseDamage = HpMax / 100 * Math.Abs(percentDefense[0]);
                    if (percentDefenseDamage < damage)
                    {
                        damage = percentDefenseDamage;
                    }
                }
                
                switch (MapMonster?.MonsterVNum)
                {
                    case 533:
                        if (63 < damage)
                        {
                            damage = 63;
                        }
                        break;

                    case 388:
                        if (200 < damage)
                        {
                            damage = 200;
                        }
                        break;
                }   
            }

            if (damager.MapEntityId != MapEntityId)
            {
                LastDefence = DateTime.Now;
            }

            Character?.DisposeShopAndExchange();

            if (Character != null)
            {
                if (Character.BattleEntity.AdditionalHp > damage)
                {
                    Character.BattleEntity.AdditionalHp -= damage;
                    damage = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
                else if (Character.BattleEntity.AdditionalHp > 0)
                {
                    damage -= (int)Character.BattleEntity.AdditionalHp;
                    Character.BattleEntity.AdditionalHp = 0;
                    Character.Session.SendPacket(Character.GenerateAdditionalHpMp());
                }
            }

            if (Mate != null && MapInstance == Mate.Owner.Miniland && Mate.MateType == MateType.Pet)
            {
                if (damager.MapMonster != null)
                {
                    if (IsMateTrainer(damager.MapMonster.MonsterVNum))
                    {
                        Mate.DefendTrainer(damager.MapMonster.MonsterVNum);
                    }
                }
            }

            if (MapMonster != null)
            {
                if (damager.Mate != null && damager.MapInstance == damager.Mate.Owner.Miniland &&
                    damager.Mate.MateType == MateType.Pet)
                {
                    if (IsMateTrainer(MapMonster.MonsterVNum))
                    {
                        damager.Mate.HitTrainer(MapMonster.MonsterVNum);
                    }
                }
            }

            if (MapMonster?.AliveTimeMp > 0)
            {
                Mp -= damage;
            }
            else
            {
                if (Hp <= damage && dontKill)
                {
                    damage = Hp - 1;
                }
                else if (Hp < damage)
                {
                    damage = Hp;
                }

                Hp -= damage;

                if (Hp > 0)
                {
                    return damage;
                }

                Hp = 0;

                if (Character != null)
                {
                    Character.WalkDisposable?.Dispose();
                    RemoveBuff(569);

                    if (MapInstance != null)
                    {
                        if (MapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                        {
                            Character.MapInstance.InstanceBag.DeadList.Add(Character.CharacterId);
                        }
                    }
                }
                else if (Mate != null)
                {
                    Mate.GenerateDeath(damager);
                }

                ClearEnemyFalcon();
            }

            return damage;
        }

        public int GetDistance(BattleEntity other) => (int)Math.Sqrt(Math.Pow(other.PositionX - PositionX, 2) + Math.Pow(other.PositionY - PositionY, 2));

        public List<MapMonster> GetOwnedMonsters()
        {
            var ownedMonsters = MapInstance?.Monsters.Where(m =>
                m.Owner?.MapEntityId == MapEntityId || Character != null &&
                Character.Mates.Any(mate => m.Owner?.MapEntityId == mate.MateTransportId)).ToList();
            return ownedMonsters;
        }

        public List<MapNpc> GetOwnedNpcs()
        {
            var ownedNpcs = MapInstance?.Npcs.Where(m =>
                m.Owner?.MapEntityId == MapEntityId || Character != null &&
                Character.Mates.Any(mate => m.Owner?.MapEntityId == mate.MateTransportId)).ToList();
            return ownedNpcs;
        }

        public MapCell GetPos() => new MapCell { X = PositionX, Y = PositionY };

        public MapCell GetRandomMapCellInRange(short numberOfCells)
        {
            if (MapInstance?.Map == null)
            {
                return null;
            }

            var walkableCellsInRange = new List<MapCell>();

            while (numberOfCells > 0)
            {
                for (var dX = -1; dX <= 1; dX++)
                    for (var dY = -1; dY <= 1; dY++)
                    {
                        if (dX == 0 && dY == 0)
                        {
                            continue;
                        }

                        var x = (short)(PositionX + dX * numberOfCells);
                        var y = (short)(PositionY + dY * numberOfCells);

                        if (!MapInstance.Map.IsBlockedZone(x, y) && MapInstance.Map.CanWalkAround(x, y))
                        {
                            walkableCellsInRange.Add(new MapCell { X = x, Y = y });
                        }
                    }

                numberOfCells--;
            }

            return walkableCellsInRange
                .OrderBy(s => ServerManager.RandomNumber(0, 1000))
                .FirstOrDefault();
        }

        public bool HasBuff(short cardId)
        {
            return Buffs.GetAllItems().Any(b => b?.Card?.CardId == cardId);
        }

        public bool HasBuff(CardType type, byte subtype)
        {
            try
            {
                return Buffs.Any(buff => buff.Card.BCards.Any(b =>
                    b.Type == (byte)type && b.SubType == subtype &&
                    (b.CastType != 1 || b.CastType == 1)));
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool HasBCard(CardType type, byte subtype)
        {
            try
            {
                return BCards.Any(b =>
                    b.Type == (byte)type && b.SubType == subtype);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public double HPLoad()
        {
            double MaxHp = 0;
            if (Character != null)
            {
                var multiplicator = 1.0;
                var hp = 0;
                if (Character.UseSp)
                {
                    var specialist =
                        Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist != null)
                    {
                        var point = CharacterHelper.SlPoint(specialist.SlHP, 3) + Character.slhpbonus;
                        if (point > 100)
                        {
                            point = 100;
                        }

                        ;

                        if (point <= 50)
                        {
                            multiplicator += point / 100.0;
                        }
                        else
                        {
                            multiplicator += 0.5 + (point - 50.00) / 50.00;
                        }

                        hp = specialist.HP + specialist.SpHP * 100;
                    }
                }

                hp += CellonOptions.Where(s => s.Type == CellonOptionType.HPMax).Sum(s => s.Value);
                multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP)[0] /
                                 100D;
                multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP)[0] / 100D;
                multiplicator +=
                    GetBuff(CardType.IncreaseHpMp, (byte)AdditionalTypes.IncreaseHpMp.IncreaseHpInPercent)[0] / 100D;

                MaxHp = (int)((CharacterHelper.HpMax(Character.Class, (byte)Level) + hp +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased)[0] +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0]) *
                               multiplicator);
            }
            else
            {
                MaxHp = HpMax;
            }

            if (Hp > MaxHp)
            {
                Hp = (int)MaxHp;
            }

            return MaxHp;
        }

        public async Task<double> HPLoadAsync()
        {
            double MaxHp = 0;
            if (Character != null)
            {
                var multiplicator = 1.0;
                var hp = 0;
                if (Character.UseSp)
                {
                    var specialist =
                        Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist != null)
                    {
                        var point = CharacterHelper.SlPoint(specialist.SlHP, 3) + Character.slhpbonus;
                        if (point > 100)
                        {
                            point = 100;
                        }

                        ;

                        if (point <= 50)
                        {
                            multiplicator += point / 100.0;
                        }
                        else
                        {
                            multiplicator += 0.5 + (point - 50.00) / 50.00;
                        }

                        hp = specialist.HP + specialist.SpHP * 100;
                    }
                }

                hp += CellonOptions.Where(s => s.Type == CellonOptionType.HPMax).Sum(s => s.Value);
                multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP)[0] /
                                 100D;
                multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP)[0] / 100D;
                multiplicator +=
                    GetBuff(CardType.IncreaseHpMp, (byte)AdditionalTypes.IncreaseHpMp.IncreaseHpInPercent)[0] / 100D;

                MaxHp =(int)((CharacterHelper.HpMax(Character.Class, (byte)Level) + hp +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased)[0] +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0]) *
                               multiplicator);
            }
            else
            {
                MaxHp = HpMax;
            }

            if (Hp > MaxHp)
            {
                Hp = (int)MaxHp;
            }

            return MaxHp;
        }

        public int HpPercent() => (int)(Hp / (double)HpMax * 100D);

        public bool IsCampfire(int vnum)
        {
            return new[] { 956, 957, 959 }.Contains(vnum);
        }

        public bool IsInRange(int xCoordinate, int yCoordinate, int range = 50) => Math.Abs(PositionX - xCoordinate) <= range && Math.Abs(PositionY - yCoordinate) <= range;

        public bool RangeIs(short mapX, short mapY, byte distance)
        {
            return Map.GetDistance(new MapCell
            {
                X = mapX,
                Y = mapY
            }, new MapCell
            {
                X = PositionX,
                Y = PositionY
            }) <= distance;
        }

        public bool IsMateTrainer(int vnum) => vnum == 160 || vnum == 900 || vnum == 636 || vnum == 971 || vnum == 1510 || vnum == 685;

        public bool IsSignpost(int vnum)
        {
            return new[] { 920, 921, 1385, 1428, 1499, 1519 }.Contains(vnum);
        }

        public int MaxTargetedByMonstersCount(bool teamCheck)
        {
            if (MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                if (teamCheck)
                {
                    switch (EntityType)
                    {
                        case EntityType.Player:
                            return GameConfiguration.MonsterToFollowPlayer;

                        case EntityType.Mate:
                            return GameConfiguration.MonsterToFollowMate;
                    }
                }
                else
                {
                    return GameConfiguration.MonsterToFollowDefault;
                }
            }
            return int.MaxValue;
        }

        public double MPLoad()
        {
            double MaxMp = 0;
            if (Character != null)
            {
                var mp = 0;
                var multiplicator = 1.0;
                if (Character.UseSp)
                {
                    var specialist =
                        Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist != null)
                    {
                        var point = CharacterHelper.SlPoint(specialist.SlHP, 3) + Character.slhpbonus;
                        if (point > 100)
                        {
                            point = 100;
                        }

                        ;

                        if (point <= 50)
                        {
                            multiplicator += point / 100.0;
                        }
                        else
                        {
                            multiplicator += 0.5 + (point - 50.00) / 50.00;
                        }

                        mp = specialist.MP + specialist.SpHP * 100;
                    }
                }

                mp += CellonOptions.Where(s => s.Type == CellonOptionType.MPMax).Sum(s => s.Value);

                multiplicator += GetBuff(CardType.BearSpirit, (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP)[0] /
                                 100D;
                multiplicator += GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP)[0] / 100D;
                multiplicator +=
                    GetBuff(CardType.IncreaseHpMp, (byte)AdditionalTypes.IncreaseHpMp.IncreaseMpInPercent)[0] / 100D;

                MaxMp = (int)((CharacterHelper.MpMax(Character.Class, (byte)Level) + mp +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased)[0] +
                                GetBuff(CardType.MaxHPMP, (byte)AdditionalTypes.MaxHPMP.MaximumHPMPIncreased)[0]) *
                               multiplicator);
            }
            else
            {
                MaxMp = MpMax;
            }

            if (Mp > MaxMp)
            {
                Mp = (int)MaxMp;
            }

            return MaxMp;
        }

        public int MpPercent() => (int)(Mp / (double)MpMax * 100D);

        public void RemoveBuff(short id, bool removePermaBuff = false)
        {
            if (!Buffs.ContainsKey(id))
            {
                return;
            }

            var indicator = Buffs[id];

            if (indicator?.Card != null)
            {
                lock (indicator)
                {
                    if (indicator.IsPermaBuff && !removePermaBuff)
                    {
                        AddBuff(indicator, this, true);
                        return;
                    }

                    Buffs.Remove(id);

                    if (indicator.Card.BCards.Any(s
                            => s.Type == (byte)CardType.SpecialAttack &&
                               s.SubType == (byte)AdditionalTypes.SpecialAttack.NoAttack
                            || s.Type == (byte)CardType.Move &&
                               s.SubType == (byte)AdditionalTypes.Move.MovementImpossible
                            || s.Type == (byte)CardType.FrozenDebuff &&
                               s.SubType == (byte)AdditionalTypes.FrozenDebuff.EternalIce
                    ))
                    {
                        if (Character != null)
                        {
                            Character.LastSpeedChange = DateTime.Now;
                            Character.LoadSpeed();
                            Character.Session?.SendPacket(Character.GenerateCond());
                        }
                        else if (Mate != null)
                        {
                            Mate.Owner?.Session?.SendPacket(Mate.GenerateCond());
                        }
                    }

                    if (indicator.Card.BCards.Any(s =>
                                s.Type == (byte)CardType.SpecialEffects &&
                                s.SubType == (byte)AdditionalTypes.SpecialEffects.ShadowAppears)
                     && GetBuff(CardType.SpecialEffects,
                                (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[] BuffData)
                    {
                        MapInstance?.Broadcast($"guri 0 {(short)UserType} {MapEntityId} {BuffData[0]} {BuffData[1]}");
                    }

                    MapInstance?.Broadcast($"bf_e {(short)UserType} {MapEntityId} {indicator.Card.CardId} 0");

                    if (Mate != null)
                    {
                        Mate.Owner?.Session?.SendPackets(Mate.Owner.GeneratePst());
                    }

                    if (Character != null)
                    {
                        if (indicator.StaticBuff)
                        {
                            Character.Session?.SendPacket($"vb {indicator.Card.CardId} 0 {indicator.Card.Duration}");
                            Character.Session?.SendPacket(Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("EFFECT_TERMINATED"),
                                            indicator.Card.Name), 11));
                        }
                        else
                        {
                            Character.Session?.SendPacket(
                                    $"bf 1 {Character.CharacterId} 0.{indicator.Card.CardId}.0 {Level}");
                            Character.Session?.SendPacket(Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("EFFECT_TERMINATED"),
                                            indicator.Card.Name), 20));
                        }

                        if (Buffs[indicator.Card.CardId] != null)
                        {
                            Buffs.Remove(id);
                        }

                        if (indicator.Card.BCards.Any(s =>
                                s.Type == (byte)CardType.Move &&
                                !s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible)))
                        {
                            Character.LastSpeedChange = DateTime.Now;
                            Character.LoadSpeed();
                            Character.Session?.SendPacket(Character.GenerateCond());
                        }

                        if (indicator.Card.BCards.Any(s =>
                                    s.Type == (byte)CardType.SpecialActions &&
                                    s.SubType.Equals((byte)AdditionalTypes.SpecialActions.Hide))
                         || indicator.Card.BCards.Any(s =>
                                    s.Type == (byte)CardType.FalconSkill &&
                                    s.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide))
                         || indicator.Card.BCards.Any(s =>
                                    s.Type == (byte)CardType.FalconSkill &&
                                    s.SubType.Equals((byte)AdditionalTypes.FalconSkill.Ambush)) || indicator.Card.BCards.Any(s =>
                                    s.Type == (byte)CardType.SummonedMonsterAttack &&
                                    s.SubType.Equals((byte)AdditionalTypes.SummonedMonsterAttack.Invisible)))
                        {
                            Character.Invisible = false;
                            foreach (var teamMate in Character.Mates?.Where(m => m != null && m.IsTeamMember))
                            {
                                teamMate.PositionX = Character.PositionX;
                                teamMate.PositionY = Character.PositionY;
                                teamMate.UpdateBushFire();

                                if (Character.MapInstance?.Sessions != null)
                                {
                                    foreach (var s in Character.MapInstance.Sessions.Where(s => s?.Character != null))
                                    {
                                        if (ServerManager.Instance.ChannelId != 51 ||
                                            Character.Faction == s.Character.Faction)
                                        {
                                            s.SendPacket(teamMate.GenerateIn(false,
                                                    ServerManager.Instance.ChannelId == 51));
                                        }
                                        else
                                        {
                                            s.SendPacket(teamMate.GenerateIn(true,
                                                    ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                        }
                                    }
                                }

                                Character.Session?.SendPacket(Character.GeneratePinit());
                                Character.Mates?.ForEach(s => Character.Session?.SendPacket(s.GenerateScPacket()));
                                Character.Session?.SendPackets(Character.GeneratePst());
                            }

                            MapInstance?.Broadcast(Character.GenerateInvisible());
                        }

                        if (indicator.Card.BCards.Any(s =>
                                s.Type == (byte)CardType.FearSkill &&
                                s.SubType.Equals((byte)AdditionalTypes.FearSkill.MoveAgainstWill)))
                        {
                            Character.Session?.SendPacket($"rv_m {MapEntityId} 1 0");
                        }

                        if (indicator.Card.BCards.Any(s =>
                                s.Type == (byte)CardType.FearSkill &&
                                s.SubType.Equals((byte)AdditionalTypes.FearSkill.AttackRangedIncreased)))
                        {
                            if (!Buffs.Any(s => s.Card.BCards.Any(b =>
                                    b.Type == (byte)CardType.FearSkill &&
                                    b.SubType.Equals((byte)AdditionalTypes.FearSkill.AttackRangedIncreased))))
                            {
                                Character.Session?.SendPacket("bf_d 0 1");
                            }
                        }

                        if (indicator.Card.BCards.Any(s => s.Type == (byte)CardType.DarkCloneSummon
                                                        && s.SubType == (byte)AdditionalTypes.DarkCloneSummon
                                                                                              .ConvertDamageToHPChance))
                        {
                            GetDamage(Character.ConvertedDamageToHP, this, true);
                            Character.ConvertedDamageToHP = 0;
                            Character.Session?.SendPacket(Character.GenerateStat());
                        }

                        // TODO : Find another way because it is hardcode

                        switch (indicator.Card.CardId)
                        {
                            case 676:
                                Character.DragonModeObservable?.Dispose();
                                break;

                            case 131:
                                Character.Session?.SendPacket(Character.GeneratePairy());
                                break;

                            case 617:
                                {
                                    Character.LastComboCastId = 0;
                                    Character.Session?.SendPacket("ms_c 1");
                                }
                                break;

                            case 620:
                                {
                                    if (Character.Session != null && Character.SavedLocation != null &&
                                        indicator.Sender?.Character?.CharacterId == Character.CharacterId)
                                    {
                                        Character.SavedLocation = null;

                                        var characterSkill = indicator.SkillVNum.HasValue
                                                ? Character.GetSkill(indicator.SkillVNum.Value)
                                                : null;

                                        var skill = characterSkill?.Skill;

                                        if (skill != null)
                                        {
                                            short cooldown = 600; // 60 seconds * 10

                                            Character.Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(skill.CastId, cooldown));

                                            characterSkill.LastUse = DateTime.Now.AddMilliseconds(cooldown * 100);

                                            Observable.Timer(characterSkill.LastUse).Subscribe(s => Character.Session.SendPacket(StaticPacketHelper.SkillReset(skill.CastId)));
                                        }
                                    }
                                }
                                break;

                            case 697:
                            case 690:
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;

                            case 727:
                            case 728:
                            case 729:
                                if (Buffs.Any(s =>
                                        s.Card.CardId == 727 || s.Card.CardId == 728 || s.Card.CardId == 729))
                                {
                                    break;
                                }

                                MapInstance.Broadcast(Character.GenerateBfePacket(indicator.Card.CardId, 0));

                                Character.UltimatePoints = 0;
                                Character.Session.SendPacket(Character.GenerateFtPtPacket());
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;

                            case 730:
                                Character.Session.SendPackets(Character.GenerateQuicklist());
                                break;

                            // The skill doesn't work like that, so I commented that code piece
                            //case 724:
                            //    Character.RemoveUltimatePoints(1000);
                            //    break;

                            case 850:
                                Character.IsFishing = false;
                                break;
                        }

                        if (indicator.Card.CardId == 559 && Character.TriggerAmbush)
                        {
                            AddBuff(new Buff(560, Character.Level), this);
                            Character.TriggerAmbush = false;
                        }
                    }

                    if (BuffObservables != null && BuffObservables.ContainsKey(indicator.Card.CardId))
                    {
                        BuffObservables[indicator.Card.CardId]?.Dispose();
                        BuffObservables.Remove(indicator.Card.CardId);
                    }

                    indicator.Card.BCards.ForEach(b => BCardDisposables[b.BCardId]?.Dispose());
                    indicator.StaticVisualEffect?.Dispose();
                }
            }
        }

        public void RemoveOwnedMonsters(bool OnlyFirst = false, int MonsterVNum = -1)
        {
            var ownedMonsters = GetOwnedMonsters()?.Where(m =>
                MonsterVNum == -1 && !IsMateTrainer(m.MonsterVNum) || m.MonsterVNum == MonsterVNum);
            if (ownedMonsters != null)
            {
                if (OnlyFirst)
                {
                    if (ownedMonsters.LastOrDefault() is MapMonster first)
                    {
                        first.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, first.MapMonsterId));
                        first.MapInstance.RemoveMonster(first);
                    }
                }
                else
                {
                    ownedMonsters.ToList().ForEach(m =>
                    {
                        m.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, m.MapMonsterId));
                        m.MapInstance.RemoveMonster(m);
                    });
                }
            }
        }

        public void RemoveOwnedNpcs(bool OnlyFirst = false, int NpcVNum = -1) 
        {
            var ownedNpcs = GetOwnedNpcs()?.Where(m => NpcVNum == -1 || m.NpcVNum == NpcVNum);
            if (ownedNpcs != null)
            {
                if (OnlyFirst)
                {
                    if (ownedNpcs.LastOrDefault() is MapNpc first)
                    {
                        first.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, first.MapNpcId));
                        first.MapInstance.RemoveNpc(first);
                    }
                }
                else
                {
                    ownedNpcs.ToList().ForEach(m =>
                    {
                        m.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, m.MapNpcId));
                        m.MapInstance.RemoveNpc(m);
                    });
                }
            }
        }

        public void SendBuffsPacket()
        {
            if (Character != null)
            {
                foreach (var indicator in Buffs.GetAllItems())
                {
                    if (!indicator.StaticBuff)
                    {
                        Character.Session.SendPacket(
                                $"bf 1 {MapEntityId} {(indicator.Card.CardId == 0 ? Character.ChargeValue > 7000 ? 7000 : Character.ChargeValue : 0)}.{indicator.Card.CardId}.{indicator.RemainingTime} {indicator.Level}");
                    }
                    else
                    {
                        if (indicator.RemainingTime == 0) Character.Session.SendPacket($"vb {indicator.Card.CardId} 1 -1");
                    }
                }
            }


        }

        public List<BattleEntity> TargettedByMonstersList(bool teamCheck)
        {
            if (!teamCheck)
            {
                return MapInstance?.Monsters
                                  .Where(s => s?.Target?.MapEntityId == MapEntityId && s.Target.EntityType == EntityType)
                                  .Select(s => s.BattleEntity).ToList();
            }

            var targettedByMonsters = new List<BattleEntity>();
            if (Mate?.Owner != null)
            {
                targettedByMonsters = Mate.Owner.BattleEntity.TargettedByMonstersList(true);
            }
            else
            {
                targettedByMonsters = MapInstance?.Monsters
                    .Where(s => s?.Target?.MapEntityId == MapEntityId && s?.Target?.EntityType == EntityType)
                    .Select(s => s.BattleEntity).ToList();
                if (Character != null)
                {
                    Character.Mates.Where(s => s.IsTeamMember).ToList().ForEach(m =>
                            targettedByMonsters.AddRange(MapInstance?.Monsters
                                                                    .Where(s => s?.Target?.MapEntityId == m.BattleEntity.MapEntityId &&
                                                                                s.Target.EntityType == m.BattleEntity.EntityType).Select(s => s.BattleEntity)
                                                                    .ToList()));
                }
            }

            return targettedByMonsters;
        }

        public void TeleportTo(MapCell mapCell, short distance = 0)
        {
            if (MapInstance?.Map == null || mapCell == null)
            {
                return;
            }

            var mapCellTo = MapInstance.Map.GetRandomPositionByDistance(mapCell.X, mapCell.Y, distance, true) ??
                            mapCell;

            PositionX = mapCellTo.X;
            PositionY = mapCellTo.Y;
            MapInstance?.Broadcast(GenerateTp());

            RemoveBuff(620);
        }   

        public void UpdateBushFire()
        {
            BrushFireJagged = BestFirstSearch.LoadBrushFireJagged(new GridPos
            {
                X = PositionX,
                Y = PositionY
            }, MapInstance.Map.JaggedGrid);
        }

        #endregion
    }
}