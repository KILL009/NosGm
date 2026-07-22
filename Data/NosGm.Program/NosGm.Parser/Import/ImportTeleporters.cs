using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportTeleporters : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportTeleporters(ImportConfiguration configuration) => _configuration = configuration;

        public void Import()
        {
            var teleporterCounter = 0;
            TeleporterDTO teleporter = null;

            var existingNpcs = DAOFactory.MapNpcDAO.LoadAll().Select(x => x.MapNpcId).ToHashSet();
            var existingTeleporters = DAOFactory.TeleporterDAO.LoadAll().ToList();

            var teleporters = new List<TeleporterDTO>();

            foreach (var currentPacket in _configuration.Packets.Where(o => o[0].Equals("at") || o[0].Equals("n_run") &&
                                                                            (o[1].Equals("16") || o[1].Equals("26") ||
                                                                             o[1].Equals("45") || o[1].Equals("301") ||
                                                                             o[1].Equals("132") ||
                                                                             o[1].Equals("5002") ||
                                                                             o[1].Equals("5012"))))
            {
                if (currentPacket.Length > 4 && currentPacket[0] == "n_run")
                {
                    if (!existingNpcs.Contains(int.Parse(currentPacket[4])))
                    {
                        teleporter = null;
                        continue;
                    }

                    teleporter = new TeleporterDTO
                    {
                        MapNpcId = int.Parse(currentPacket[4]),
                        Index = short.Parse(currentPacket[2])
                    };
                    continue;
                }

                if (currentPacket.Length <= 5 || currentPacket[0] != "at")
                {
                    continue;
                }

                if (teleporter == null)
                {
                    continue;
                }

                teleporter.MapId = short.Parse(currentPacket[2]);
                teleporter.MapX = short.Parse(currentPacket[3]);
                teleporter.MapY = short.Parse(currentPacket[4]);

                if (existingTeleporters.Any(x => x.MapNpcId == teleporter.MapNpcId && x.Index == teleporter.Index))
                {
                    continue;
                }

                teleporters.Add(teleporter);
                teleporterCounter++;
                teleporter = null;
            }

            DAOFactory.TeleporterDAO.Insert(teleporters);
            Logger.Log.Info($"{teleporterCounter} Teleporters parsed");
        }
    }
}