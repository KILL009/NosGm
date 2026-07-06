using Frostvein.DAL;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadCharacterRelations
    {
        public static void Load()
        {
            ServerManager.Instance.CharacterRelations = DAOFactory.CharacterRelationDAO.LoadAll().ToList();
            ServerManager.Instance.PenaltyLogs = DAOFactory.PenaltyLogDAO.LoadAll().ToList();
        }
    }
}
