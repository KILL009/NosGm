using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Event.Handler
{
    public static class GemStoneEvent
    {
        public static void Load()
        {
            if (DAOFactory.MapDAO.LoadById(2107) == null)
            {
                return;
            }

            var portal = new Portal
            {
                SourceMapId = 2107,
                SourceX = 10,
                SourceY = 5,
                DestinationMapId = 1,
                DestinationX = 0,
                DestinationY = 0,
                Type = -1
            };

            void loadSpecialistGemMap(short npcVNum)
            {
                MapInstance specialistGemMapInstance;
                specialistGemMapInstance = ServerManager.GenerateMapInstance(2107, MapInstanceType.GemmeStoneInstance,
                    new InstanceBag());
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
