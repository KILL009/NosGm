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
    public static class PluginLoadCards
    {
        public static void Load()
        {
            ServerManager.Cards = new List<Card>();

            var bcards = DAOFactory.BCardDAO.LoadAll().ToArray().Where(s => s.CardId.HasValue);
            IEnumerable<CardDTO> cards = DAOFactory.CardDAO.LoadAll().ToArray();
            foreach (var card in cards)
            {
                var tmp = new Card(card)
                {
                    BCards = new List<BCard>()
                };

                foreach (var bcard in bcards.Where(s => s.CardId == tmp.CardId))
                {
                    tmp.BCards.Add(new BCard(bcard));
                }

                ServerManager.Cards.Add(tmp);
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Cards.Count} Cards - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
