using Game.Configuration.BCards;
using NosGm.Domain;
using System;
using System.Threading.Tasks;

namespace NosGm.GameObject._plugins.BCards.Handler
{
    public class MoveHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Move;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            if (target.Character == null)
            {
                return;
            }

            target.Character.LastSpeedChange = DateTime.Now;
            target.Character.LoadSpeed();
            target.Character.Session?.SendPacket(target.Character.GenerateCond());
        }
    }
}