using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Frostvein.Parser.Import
{
    public class ImportMaps : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportMaps(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string FileMapIdDat => Path.Combine(_configuration.DatFolder, "MapIDData.dat");

        private string FileMapIdLang =>
            Path.Combine(_configuration.LangFolder, $"_code_{_configuration.Lang}_MapIDData.txt");

        public void Import()
        {
            var existingMaps = DAOFactory.MapDAO.LoadAll().Select(x => x.MapId).ToHashSet();

            var maps = new List<MapDTO>();
            var dictionaryId = new Dictionary<int, string>();
            var dictionaryMusic = new Dictionary<int, int>();
            var dictionaryMusic2 = new Dictionary<int, int>();
            var dictionaryIdLang = new Dictionary<string, string>();
            int MusicCount = 0;
            var i = 0;
            using (var mapIdStream = new StreamReader(FileMapIdDat, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = mapIdStream.ReadLine()) != null)
                {
                    var values = line.Split(' ');
                    if (values.Length <= 1) continue;

                    if (!int.TryParse(values[0], out var mapId)) continue;

                    if (!dictionaryId.ContainsKey(mapId)) dictionaryId.Add(mapId, values[4]);
                }
            }

            using (var mapIdLangStream = new StreamReader(FileMapIdLang, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = mapIdLangStream.ReadLine()) != null)
                {
                    var linesave = line.Split('\t');
                    if (linesave.Length <= 1 || dictionaryIdLang.ContainsKey(linesave[0])) continue;
                    dictionaryIdLang.Add(linesave[0], linesave[1]);
                }
            }

            foreach (var atPacket in _configuration.Packets.Where(o => o[0].Equals("at")))
            {
                if (atPacket.Length > 7 && !dictionaryMusic.ContainsKey(int.Parse(atPacket[2])))
                {
                    dictionaryMusic[int.Parse(atPacket[2])] = int.Parse(atPacket[7]);
                }
            }

            foreach (var file in new DirectoryInfo(_configuration.MapFolder).GetFiles())
            {
                addMap(short.Parse(file.Name), short.Parse(file.Name), File.ReadAllBytes(file.FullName));
            }

            void addMap(short mapId, short originalMapId, byte[] mapData)
            {
                string name = "";
                int music = 0;

                if (dictionaryId.ContainsKey(mapId) && dictionaryIdLang.ContainsKey(dictionaryId[mapId]))
                {
                    name = dictionaryIdLang[dictionaryId[mapId]];
                }

                if (dictionaryMusic.ContainsKey(mapId))
                {
                    music = dictionaryMusic[mapId];
                }
                else
                {
                    switch (mapId)
                    {
                        case 265: music = 193; break;
                        case 266: music = 194; break;
                        case 267: music = 196; break;
                        case 268: music = 197; break;
                        case 269: music = 198; break;
                        case 270: music = 199; break;
                        case 271: music = 200; break;
                        case 272: music = 201; break;
                        case 273: music = 202; break;
                        case 274: music = 203; break;
                        case 275: music = 204; break;
                        case 276: music = 205; break;
                        case 277: music = 206; break;
                        case 278: music = 207; break;
                        case 279: music = 208; break;
                        case 280: music = 209; break;
                        case 281: music = 195; break;
                        case 282: music = 210; break;
                        case 283: music = 211; break;
                        case 2500: music = 31; break;
                        case 2501: music = 31; break;
                        case 2502: music = 31; break;
                        case 2510: music = 12; break;
                        case 2511: music = 12; break;
                        case 2512: music = 12; break;
                        case 2520: music = 16; break;
                        case 2521: music = 16; break;
                        case 2522: music = 16; break;
                        case 2530: music = 78; break;
                        case 2531: music = 78; break;
                        case 2532: music = 78; break;
                        case 2542: music = 182; break;
                        case 2536: 
                        case 2540:
                        case 2541:
                        case 2550:
                        case 2551:
                        case 2552:
                        case 2553:
                        case 2556:
                        case 2590:
                        case 2600:
                        case 2601:
                        case 2603:
                        case 2604:
                        case 2628:
                        case 2629:
                        case 2630:
                        case 2631:
                        case 2632:
                        case 2633:
                        case 2634:
                        case 2635:
                        case 2636:
                        case 2637:
                        case 2638:
                        case 2639:
                        case 2640:
                        case 2641:
                        case 2642:
                        case 2643:
                        case 2644:
                        case 2645:
                        case 2646:
                        case 2647:
                        case 2648:
                        case 2649:
                        case 2650:
                        case 2700:
                        case 2701:
                        case 2702:
                        case 2703:
                        case 2704:
                        case 2705:
                        case 2706:
                        case 2707:
                        case 2708:
                        case 2709:
                        case 2710:
                        case 2711:
                        case 2712:
                        case 2713:
                        case 2715:
                        case 2716:
                        case 2717:
                        case 2750:
                        case 2751:
                        case 2651:
                            break;
                    }
                }

                if (existingMaps.Contains(mapId))
                {
                    return;
                }

                var map = new MapDTO
                {
                    Name = name,
                    Music = music,
                    MapId = mapId,
                    GridMapId = originalMapId,
                    Data = mapData,
                    ShopAllowed = mapId == 147
                };

                if (maps.Contains(maps.FirstOrDefault(s => s.MapId == map.MapId)))
                {
                    return;
                }

                maps.Add(map);
                i++;
            }

            DAOFactory.MapDAO.Insert(maps);
            Logger.Log.Info($"{i} Maps parsed");
        }
    }
}