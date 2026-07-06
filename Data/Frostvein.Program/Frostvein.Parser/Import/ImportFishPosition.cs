using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Frostvein.Parser.Import
{
    public class ImportFishPosition : IImport
    {
        private const string FileName = "fish.dat";

        private readonly ImportConfiguration _configuration;

        public ImportFishPosition(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Import()
        {
            var fileFishDat = Path.Combine(_configuration.DatFolder, FileName);
            var fileItemDat = Path.Combine(_configuration.DatFolder, "Item.dat");
            var fileItemLang = Path.Combine(_configuration.LangFolder, $"_code_{_configuration.Lang}_Item.txt");

            List<short> currentMapIds = new List<short>();
            List<FishingPositionDto> fishingSpots = new List<FishingPositionDto>();
            List<FishingInformationsDto> fishInfo = new List<FishingInformationsDto>();
            Dictionary<string, string> languageKeys = new Dictionary<string, string>();
            string line;
            short currentLevel = 0;
            short mapId = 0;

            using (var fishDatReader = new StreamReader(fileFishDat, Encoding.GetEncoding(1252)))
            {
                while ((line = fishDatReader.ReadLine()) != null)
                {
                    var split = line.Split('\t');

                    if (split.Length <= 1)
                    {
                        continue;
                    }

                    switch (split[0])
                    {
                        case "VNUM":
                            currentMapIds.Clear();
                            break;
                        case "LEVEL":
                            currentLevel = short.Parse(split[1]);
                            break;
                        case "MAP":
                            mapId = short.Parse(split[2]);
                            currentMapIds.Add(mapId);
                            break;
                        case "POS":
                            fishingSpots.Add(new FishingPositionDto
                            {
                                MapId = mapId,
                                MinLevel = currentLevel,
                                MapX = short.Parse(split[3]),
                                MapY = short.Parse(split[4]),
                                Direction = short.Parse(split[5])
                            });
                            break;
                        case "ITEM":
                            fishInfo.Add(new FishingInformationsDto
                            {
                                FishVNum = short.Parse(split[2]),
                                Probability = short.Parse(split[3]),
                                MapId1 = (short)(currentMapIds.Count >= 1 ? currentMapIds[0] : 0),
                                MapId2 = (short)(currentMapIds.Count >= 2 ? currentMapIds[1] : 0),
                                MapId3 = (short)(currentMapIds.Count >= 3 ? currentMapIds[2] : 0),
                                IsFish = true
                            });
                            break;
                        case "BASIC":
                            fishInfo.Add(new FishingInformationsDto
                            {
                                FishVNum = short.Parse(split[2]),
                                Probability = short.Parse(split[3]),
                                MapId1 = (short)(currentMapIds.Count >= 1 ? currentMapIds[0] : 0),
                                MapId2 = (short)(currentMapIds.Count >= 2 ? currentMapIds[1] : 0),
                                MapId3 = (short)(currentMapIds.Count >= 3 ? currentMapIds[2] : 0),
                                IsFish = false
                            });
                            break;
                    }
                }
            }

            using (StreamReader itemLanguageStream = new StreamReader(fileItemLang, Encoding.GetEncoding(1252)))
            {
                while ((line = itemLanguageStream.ReadLine()) != null)
                {
                    string[] currentLine = line.Split('\t');
                    if (currentLine.Length <= 1 || languageKeys.ContainsKey(currentLine[0]))
                    {
                        continue;
                    }

                    languageKeys.Add(currentLine[0], currentLine[1]);
                }
            }

            using (StreamReader itemDatReader = new StreamReader(fileItemDat, Encoding.GetEncoding(1252)))
            {
                int currentVNum = 0;
                string currentKey = string.Empty;
                while ((line = itemDatReader.ReadLine()) != null)
                {
                    var split = line.Split('\t');

                    if (split.Length > 3 && split[1] == "VNUM")
                    {
                        currentVNum = short.Parse(split[2]);
                    }
                    if (split.Length == 1 && !string.IsNullOrEmpty(split[0]))
                    {
                        currentKey = split[0];
                    }

                    if (string.IsNullOrEmpty(currentKey) || !languageKeys.ContainsKey(currentKey) || currentVNum < 10000)
                    {
                        continue;
                    }

                    var currentFish = fishInfo.Where(s => s.FishVNum == currentVNum).ToList();
                    if (!currentFish.Any())
                    {
                        continue;
                    }

                    var description = languageKeys[currentKey];
                    var descLast = description.Split(' ').Last().Replace("cm[n]", string.Empty);
                    var sizeSplit = descLast.Split('-');

                    if (sizeSplit.Length < 2)
                    {
                        continue;
                    }

                    foreach (var fish in currentFish)
                    {
                        fish.MinFishLength = (short)Math.Floor(double.Parse(sizeSplit[0]));
                        fish.MaxFishLength = (short)Math.Floor(double.Parse(sizeSplit[1]));
                    }
                    currentVNum = 0;
                    currentKey = string.Empty;
                }
            }

            DAOFactory.FishingPositionDao.InsertOrUpdate(fishingSpots);
            DAOFactory.FishingInformationDao.InsertorUpdate(fishInfo);
            Logger.Info($"{fishingSpots.Count} fishing spots saved");
            Logger.Info($"{fishInfo.Count} fish info saved");
        }
    }
}
