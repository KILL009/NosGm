using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class GemStoneEvent
    {
        private const short SpecialistGemMapId = 2107;

        public static void Load()
        {
            if (DAOFactory.MapDAO.LoadById(SpecialistGemMapId) == null)
            {
                return;
            }

            if (!ServerManager.Maps.Any(map => map.MapId == SpecialistGemMapId))
            {
                LoggerService.LogServer.Logger.LogAsync(
                    $"[GEM_STONE_EVENT_SKIPPED] Runtime map {SpecialistGemMapId} was not loaded.",
                    LogType.ERROR);
                return;
            }

            var portal = new Portal
            {
                SourceMapId = SpecialistGemMapId,
                SourceX = 10,
                SourceY = 5,
                DestinationMapId = 1,
                DestinationX = 0,
                DestinationY = 0,
                Type = -1
            };

            void loadSpecialistGemMap(short npcVNum)
            {
                var specialistGemMapInstance = ServerManager.GenerateMapInstance(
                    SpecialistGemMapId,
                    MapInstanceType.GemmeStoneInstance,
                    new InstanceBag());

                if (specialistGemMapInstance == null)
                {
                    LoggerService.LogServer.Logger.LogAsync(
                        $"[GEM_STONE_EVENT_SKIPPED] Could not create runtime map {SpecialistGemMapId} for NPC {npcVNum}.",
                        LogType.ERROR);
                    return;
                }

                specialistGemMapInstance.Npcs.Where(s => s.NpcVNum != npcVNum).ToList()
                    .ForEach(s => specialistGemMapInstance.RemoveNpc(s));
                specialistGemMapInstance.CreatePortal(portal);
                ServerManager.Instance.SpecialistGemMapInstances.Add(specialistGemMapInstance);
            }

            loadSpecialistGemMap(932); // Pajama
            loadSpecialistGemMap(933); // SP 1
            loadSpecialistGemMap(934); // SP 2
            loadSpecialistGemMap(948); // SP 3
            loadSpecialistGemMap(954); // SP 4
            loadSpecialistGemMap(958); // ?
        }
    }
}