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
    public static class FamilyArenaEvent
    {
        public static void Load()
        {
            if (DAOFactory.MapDAO.LoadById(2106) == null)
            {
                return;
            }

            ServerManager.Instance.FamilyArenaInstance = ServerManager.GenerateMapInstance(2106, MapInstanceType.ArenaInstance, new InstanceBag());
            ServerManager.Instance.FamilyArenaInstance.IsPVP = true;

            var portal = new Portal
            {
                SourceMapId = 2106,
                SourceX = 37,
                SourceY = 69,
                DestinationMapId = 1,
                DestinationX = 0,
                DestinationY = 0,
                Type = -1
            };

            ServerManager.Instance.FamilyArenaInstance.CreatePortal(portal);
        }
    }
}
