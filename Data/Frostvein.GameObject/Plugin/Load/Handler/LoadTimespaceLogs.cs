using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadTimespaceLogs
    {
        public static void Load()
        {
            ServerManager.Instance.TimespaceLogs = DAOFactory.CharacterTimeSpaceLogDAO.LoadAll().ToList();
        }
    }
}
