using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadBannedCharacters
    {
        public static void Load()
        {
            ServerManager.Instance.BannedCharacters.Clear();
            DAOFactory.CharacterDAO.LoadAll().ToList().ForEach(s =>
            {
                if (s.State != CharacterState.Active || DAOFactory.PenaltyLogDAO.LoadByAccount(s.AccountId)
                        .Any(c => c.DateEnd > DateTime.Now && c.Penalty == PenaltyType.Banned))
                {
                    ServerManager.Instance.BannedCharacters.Add(s.CharacterId);
                }
            });
        }
    }
}
