using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Parser.Import
{
    public class ImportMapTypeMap : IImport
    {
        public void Import()
        {
            var existingMaps = DAOFactory.MapDAO.LoadAll().Select(x => x.MapId).ToHashSet();
            var existingMapTypes = DAOFactory.MapTypeMapDAO.LoadAll().ToList();

            var maptypemaps = new List<MapTypeMapDTO>();
            short mapTypeId = 1;
            for (var i = 1; i < 300; i++)
            {
                var objectset = false;
                if (i < 3 || i > 48 && i < 53 || i > 67 && i < 76 || i == 102 || i > 103 && i < 105 ||
                    i > 144 && i < 149)
                {
                    // "act1"
                    mapTypeId = (short)MapTypeEnum.Act1;
                    objectset = true;
                }
                else if (i > 19 && i < 34 || i > 52 && i < 68 || i > 84 && i < 101)
                {
                    // "act2"
                    mapTypeId = (short)MapTypeEnum.Act2;
                    objectset = true;
                }
                else if (i > 40 && i < 45 || i > 45 && i < 48 || i > 99 && i < 102 || i > 104 && i < 128)
                {
                    // "act3"
                    mapTypeId = (short)MapTypeEnum.Act3;
                    objectset = true;
                }
                else if (i == 260)
                {
                    // "act3.2"
                    mapTypeId = (short)MapTypeEnum.Act32;
                    objectset = true;
                }
                else if (i > 129 && i <= 134 || i == 135 || i == 137 || i == 139 || i == 141 || i > 150 && i < 155)
                {
                    // "act4"
                    mapTypeId = (short)MapTypeEnum.Act4;
                    objectset = true;
                }
                else if (i > 169 && i < 205)
                {
                    // "act5.1"
                    mapTypeId = (short)MapTypeEnum.Act51;
                    objectset = true;
                }
                else if (i > 204 && i < 221)
                {
                    // "act5.2"
                    mapTypeId = (short)MapTypeEnum.Act52;
                    objectset = true;
                }
                else if (i > 227 && i < 241)
                {
                    // "act6.1"
                    mapTypeId = (short)MapTypeEnum.Act61;
                    objectset = true;
                }
                else if (i > 239 && i < 251 || i == 299)
                {
                    // "act6.2"
                    mapTypeId = (short)MapTypeEnum.Act62;
                    objectset = true;
                }
                else if (i == 103)
                {
                    // "Comet plain"
                    mapTypeId = (short)MapTypeEnum.CometPlain;
                    objectset = true;
                }
                else if (i == 6)
                {
                    // "Mine1"
                    mapTypeId = (short)MapTypeEnum.Mine1;
                    objectset = true;
                }
                else if (i > 6 && i < 9)
                {
                    // "Mine2"
                    mapTypeId = (short)MapTypeEnum.Mine2;
                    objectset = true;
                }
                else if (i == 3)
                {
                    // "Meadown of mine"
                    mapTypeId = (short)MapTypeEnum.MeadowOfMine;
                    objectset = true;
                }
                else if (i == 4)
                {
                    // "Sunny plain"
                    mapTypeId = (short)MapTypeEnum.SunnyPlain;
                    objectset = true;
                }
                else if (i == 5)
                {
                    // "Fernon"
                    mapTypeId = (short)MapTypeEnum.Fernon;
                    objectset = true;
                }
                else if (i > 9 && i < 19 || i > 79 && i < 85)
                {
                    // "FernonF"
                    mapTypeId = (short)MapTypeEnum.FernonF;
                    objectset = true;
                }
                else if (i > 75 && i < 79)
                {
                    // "Cliff"
                    mapTypeId = (short)MapTypeEnum.Cliff;
                    objectset = true;
                }
                else if (i == 150)
                {
                    // "Land of the dead"
                    mapTypeId = (short)MapTypeEnum.LandOfTheDead;
                    objectset = true;
                }
                else if (i == 138)
                {
                    // "Cleft of Darkness"
                    mapTypeId = (short)MapTypeEnum.CleftOfDarkness;
                    objectset = true;
                }
                else if (i == 9305)
                {
                    // "PVPMap"
                    mapTypeId = (short)MapTypeEnum.PVPMap;
                    objectset = true;
                }
                else if (i == 130 && i == 131)
                {
                    // "Citadel"
                    mapTypeId = (short)MapTypeEnum.Citadel;
                    objectset = true;
                }

                // add "act6.1a" and "act6.1d" when ids found
                if (objectset && existingMaps.Contains((short)i) &&
                    !existingMapTypes.Any(x => x.MapId == (short)i && x.MapTypeId == mapTypeId))
                    maptypemaps.Add(new MapTypeMapDTO { MapId = (short)i, MapTypeId = mapTypeId });
            }

            DAOFactory.MapTypeMapDAO.Insert(maptypemaps);
            Logger.Log.Info($"{maptypemaps.Count} MapTypeMaps parsed");
        }
    }
}