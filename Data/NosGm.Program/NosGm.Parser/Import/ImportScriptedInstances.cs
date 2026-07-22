using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Parser.Import
{
    public class ImportScriptedInstances : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportScriptedInstances(ImportConfiguration importConfiguration)
        {
            _configuration = importConfiguration;
        }

        public void Import()
        {
            short map = 0;

            var listtimespace = new List<ScriptedInstanceDTO>();
            var bddlist = new List<ScriptedInstanceDTO>();

            foreach (var currentPacket in _configuration.Packets.Where(o =>
                o[0].Equals("c_map") || o[0].Equals("wp") || o[0].Equals("gp") || o[0].Equals("rbr")))
            {
                if (currentPacket.Length > 3 && currentPacket[0] == "c_map")
                {
                    map = short.Parse(currentPacket[2]);
                    bddlist = DAOFactory.ScriptedInstanceDAO.LoadByMap(map).ToList();
                    continue;
                }

                if (DAOFactory.MapDAO.LoadById(map) == null)
                {
                    continue;
                }

                if (currentPacket.Length > 6 && currentPacket[0] == "wp")
                {
                    var ts = new ScriptedInstanceDTO
                    {
                        PositionX = short.Parse(currentPacket[1]),
                        PositionY = short.Parse(currentPacket[2]),
                        MapId = map
                    };

                    if (!bddlist.Concat(listtimespace).Any(s =>
                        s.MapId == ts.MapId && s.PositionX == ts.PositionX && s.PositionY == ts.PositionY))
                        listtimespace.Add(ts);
                }
                else
                {
                    switch (currentPacket[0])
                    {
                        case "gp":
                            if (sbyte.Parse(currentPacket[4]) == (byte)PortalType.Raid)
                            {
                                var ts = new ScriptedInstanceDTO
                                {
                                    PositionX = short.Parse(currentPacket[1]),
                                    PositionY = short.Parse(currentPacket[2]),
                                    MapId = map,
                                    Type = ScriptedInstanceType.Raid
                                };

                                if (!bddlist.Concat(listtimespace).Any(s =>
                                    s.MapId == ts.MapId && s.PositionX == ts.PositionX && s.PositionY == ts.PositionY))
                                    listtimespace.Add(ts);
                            }

                            break;
                        case "rbr":
                            //someinfo
                            break;
                    }
                }
            }

            var zenasRaid = new ScriptedInstanceDTO
            {
                Label = "Zenas",
                MapId = 232,
                PositionX = 103,
                PositionY = 125,
                Type = ScriptedInstanceType.RaidAct6
            };

            if (!DAOFactory.ScriptedInstanceDAO.LoadByMap(zenasRaid.MapId).Any()) listtimespace.Add(zenasRaid);

            var ereniaRaid = new ScriptedInstanceDTO
            {
                Label = "Erenia",
                MapId = 236,
                PositionX = 130,
                PositionY = 117,
                Type = ScriptedInstanceType.RaidAct6
            };

            if (!DAOFactory.ScriptedInstanceDAO.LoadByMap(ereniaRaid.MapId).Any()) listtimespace.Add(ereniaRaid);

            DAOFactory.ScriptedInstanceDAO?.Insert(listtimespace);

            Logger.Log.Info($"{listtimespace.Count} Timespaces parsed");
        }
    }
}