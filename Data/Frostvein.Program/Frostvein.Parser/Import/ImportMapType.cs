using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Parser.Import
{
    public class ImportMapType : IImport
    {
        #region Methods

        public void Import()
        {
            var existingMapTypes = DAOFactory.MapTypeDAO.LoadAll().ToList();

            var mapTypes = new List<MapTypeDTO>
            {
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act1,
                    MapTypeName = "Act1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act2,
                    MapTypeName = "Act2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act3,
                    MapTypeName = "Act3",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act4,
                    MapTypeName = "Act4",
                    PotionDelay = 5000
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act51,
                    MapTypeName = "Act5.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct5
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act52,
                    MapTypeName = "Act5.2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct5,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct5
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act61,
                    MapTypeName = "Act6.1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act62,
                    MapTypeName = "Act6.2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act61a,
                    MapTypeName = "Act6.1a", // angel camp
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act61d,
                    MapTypeName = "Act6.1d", // demon camp
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct6,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.CometPlain,
                    MapTypeName = "CometPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Mine1,
                    MapTypeName = "Mine1",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Mine2,
                    MapTypeName = "Mine2",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.MeadowOfMine,
                    MapTypeName = "MeadownOfPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.SunnyPlain,
                    MapTypeName = "SunnyPlain",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Fernon,
                    MapTypeName = "Fernon",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.FernonF,
                    MapTypeName = "FernonF",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Cliff,
                    MapTypeName = "Cliff",
                    PotionDelay = 300,
                    RespawnMapTypeId = (long) RespawnType.DefaultAct1,
                    ReturnMapTypeId = (long) RespawnType.ReturnAct1
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.LandOfTheDead,
                    MapTypeName = "LandOfTheDead",
                    PotionDelay = 300
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Act32,
                    MapTypeName = "Act 3.2",
                    PotionDelay = 300
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.CleftOfDarkness,
                    MapTypeName = "Cleft of Darkness",
                    PotionDelay = 300
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.PVPMap,
                    MapTypeName = "PVPMap",
                    PotionDelay = 300
                },
                new MapTypeDTO
                {
                    MapTypeId = (short) MapTypeEnum.Citadel,
                    MapTypeName = "Citadel",
                    PotionDelay = 300
                }
            };

            mapTypes.RemoveAll(x => existingMapTypes.Any(s => s.MapTypeName == x.MapTypeName));
            DAOFactory.MapTypeDAO.Insert(mapTypes);
            Logger.Log.Info($"{mapTypes.Count} MapTypes parsed");
        }

        #endregion
    }
}