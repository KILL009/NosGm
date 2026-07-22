using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.GameObject._plugins.BCards.Handler
{
    public class ReflectionHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Reflection;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            var firstData = evnt.FirstData;
            var secondData = evnt.BCard.SecondData;
            var subType = evnt.BCard.SubType;

            if (ServerManager.RandomNumber() >= firstData)
            {
                return;
            }

            switch (subType)
            {
                case (byte)AdditionalTypes.Reflection.EnemyMPDecreased:
                    target.DecreaseMp(target.Mp * secondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyMPIncreased:
                    target.IncreaseMp(target.Mp * secondData / 100);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPDecreased:
                    target.GetDamage(target.Hp * secondData / 100, target);
                    break;
                case (byte)AdditionalTypes.Reflection.EnemyHPIncreased:
                    target.IncreaseHp(target.Hp * secondData / 100);
                    break;
            }

            if (target.Character != null)
            {
                target.Character.Session.SendPacket(target.Character.GenerateStat());
            }
        }
    }
}
