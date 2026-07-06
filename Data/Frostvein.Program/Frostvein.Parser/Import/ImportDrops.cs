using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System.Collections.Generic;

namespace Frostvein.Parser.Import
{
    public class ImportDrops : IImport
    {
        public void Import()
        {
            var drops = new List<DropDTO>();

            AddGlobalDrops(drops);
            AddAct1Drops(drops);
            AddAct2Drops(drops);
            AddAct3Drops(drops);
            AddAct32Drops(drops);
            AddAct34Drops(drops);
            AddAct4Drops(drops);
            AddAct42Drops(drops);
            AddAct5Drops(drops);
            AddAct52Drops(drops);
            AddAct61AngelDrops(drops);
            AddAct61DemonDrops(drops);
            AddAct62Drops(drops);
            AddCometPlainDrops(drops);
            AddMine1Drops(drops);
            AddMine2Drops(drops);
            AddMeadownOfMineDrops(drops);
            AddSunnyPlainDrops(drops);
            AddFernonDrops(drops);
            AddFernonFDrops(drops);
            AddCliffDrops(drops);
            AddLandOfDeathDrops(drops);

            AddAct6Drops(drops);

            Logger.Log.Info($"{drops.Count} Drops parsed");
            DAOFactory.DropDAO.Insert(drops);
        }

        private void AddGlobalDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO { Amount = 1, DropChance = 1, ItemVNum = 2282 });
            drops.Add(new DropDTO { Amount = 1, DropChance = 1, ItemVNum = 1030 });
        }

        private static void AddAct6Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2803,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2804,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2805,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 350,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 350,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act61
            });
        }

        private static void AddLandOfDeathDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1019,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1020,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1021,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1022,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1211,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.LandOfTheDead
            });
        }

        private static void AddCliffDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Cliff
            });
        }

        private static void AddFernonFDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.FernonF
            });
        }

        private static void AddFernonDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 9000,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Fernon
            });
        }

        private static void AddMine1Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Mine1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Mine1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short)MapTypeEnum.Mine1
            });
        }

        private static void AddMine2Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 11000,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Mine2
            });
        }

        private static void AddMeadownOfMineDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1005,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 10000,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.MeadowOfMine
            });
        }

        private static void AddSunnyPlainDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1003,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1006,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.SunnyPlain
            });
        }

        private static void AddCometPlainDrops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.CometPlain
            });
        }

        private static void AddAct62Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1191,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1192,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1193,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1194,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2452,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2453,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2454,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2455,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2456,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act62
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act62
            });
        }

        private static void AddAct61DemonDrops(List<DropDTO> drops)
        {
            /*
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61D
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5881,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61D
            });*/
        }

        private static void AddAct61AngelDrops(ICollection<DropDTO> drops)
        {
            /*
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2000,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2446,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2806,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2807,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2813,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2815,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2816,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2818,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2819,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act61A
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5880,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act61A
            });*/
        }

        private static void AddAct52Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 600,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 5000,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1092,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1093,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1094,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1200,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2380,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short)MapTypeEnum.Act52
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act52
            });
        }

        private static void AddAct5Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1872,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1873,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1874,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2351,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2379,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act51
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act51
            });
        }

        private static void AddAct42Drops(List<DropDTO> drops)
        {
            /* drops.Add(new DropDTO
             {
                 ItemVNum = 1004,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 1000,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1007,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 1000,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1010,
                 Amount = 3,
                 MonsterVNum = null,
                 DropChance = 1500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1012,
                 Amount = 2,
                 MonsterVNum = null,
                 DropChance = 3000,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1241,
                 Amount = 3,
                 MonsterVNum = null,
                 DropChance = 3000,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1078,
                 Amount = 3,
                 MonsterVNum = null,
                 DropChance = 1500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1246,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 2500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1247,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 2500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1248,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 2500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 1429,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 2500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2296,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 1000,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2307,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 1500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2308,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 1500,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2445,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2448,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2449,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2450,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 2451,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });
             drops.Add(new DropDTO
             {
                 ItemVNum = 5986,
                 Amount = 1,
                 MonsterVNum = null,
                 DropChance = 700,
                 MapTypeId = (short)MapTypeEnum.Act42
             });*/
        }

        private static void AddAct4Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1010,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 2,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 3,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1246,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1247,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1248,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1429,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2307,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2308,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1500,
                MapTypeId = (short)MapTypeEnum.Act4
            });
        }

        private static void AddAct34Drops(List<DropDTO> drops)
        {
            /*
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3000,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5999,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Oasis
            });*/
        }

        private static void AddAct32Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 6000,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 20,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 60,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 40,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 3500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2600,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2605,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5857,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 50,
                MapTypeId = (short)MapTypeEnum.Act32
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act32
            });
        }

        private static void AddAct3Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 8000,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1078,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1235,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1238,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1240,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 400,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 800,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 300,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 4000,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 700,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 350,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2285,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 150,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5853,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5854,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5855,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act3
            });
        }

        private static void AddAct2Drops(List<DropDTO> drops)
        {
            // Act2
            drops.Add(new DropDTO
            {
                ItemVNum = 1004,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1007,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 7000,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1086,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1237,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1239,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1241,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2098,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2099,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2100,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2101,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2102,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2114,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2115,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2116,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2117,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2118,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 900,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2129,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2205,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2206,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2207,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2208,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 200,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2282,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 2500,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2283,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 1000,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2284,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 500,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2296,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 250,
                MapTypeId = (short)MapTypeEnum.Act2
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 5119,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 30,
                MapTypeId = (short)MapTypeEnum.Act2
            });
        }

        private static void AddAct1Drops(List<DropDTO> drops)
        {
            drops.Add(new DropDTO
            {
                ItemVNum = 1002,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 1012,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 12000,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2015,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2016,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2023,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2024,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
            drops.Add(new DropDTO
            {
                ItemVNum = 2028,
                Amount = 1,
                MonsterVNum = null,
                DropChance = 100,
                MapTypeId = (short)MapTypeEnum.Act1
            });
        }
    }
}