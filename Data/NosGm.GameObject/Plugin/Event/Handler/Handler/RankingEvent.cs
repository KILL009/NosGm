using NosGm.DAL;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class RankingEvent
    {
        public static void Load()
        {
            ServerManager.Instance.TopComplimented = DAOFactory.CharacterDAO.GetTopCompliment();
            ServerManager.Instance.TopPoints = DAOFactory.CharacterDAO.GetTopPoints();
            ServerManager.Instance.TopReputation = DAOFactory.CharacterDAO.GetTopReputation();
            ServerManager.Instance.TopDuel = DAOFactory.CharacterDAO.GetTopDuel();
            ServerManager.Instance.TopMonster = DAOFactory.CharacterDAO.GetTopMonster();
        }
    }
}
