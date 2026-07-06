using Game.Configuration.BCards;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;

namespace Frostvein.GameObject._plugins.BCards.Handler
{
    public class ReflectionHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Reflection;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            var firstData = evnt.FirstData;
            var SecondData = evnt.BCard.SecondData;
            var SubType = evnt.BCard.SubType;

            if (ServerManager.RandomNumber() >= firstData)
            {
                return;
            }

            switch (SubType)
            {
                case (byte)AdditionalTypes.Reflection.EnemyMPDecreased:
                    target.DecreaseMp(target.Mp * SecondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyMPIncreased:
                    target.IncreaseMp(target.Mp * SecondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPDecreased:
                    target.GetDamage(target.Mp * SecondData / 100, target);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPIncreased:
                    target.IncreaseHp(target.Mp * SecondData / 100);
                    break;
            }
            if (target.Character != null)
            {
                target.Character.Session.SendPacket(target.Character.GenerateStat());
            }
        }
    }
}