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
    public static class MoritiusShipEvent
    {
        public static void Load()
        {
            if (DAOFactory.MapDAO.LoadById(2629) == null)
            {
                return;
            }

            ServerManager.Instance.Act7Ship = ServerManager.GenerateMapInstance(2629, MapInstanceType.Act7Ship, new InstanceBag());
        }
    }
}
