using Game.Configuration.BCards;
using NosGm.Domain;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class BearSpiritHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.BearSpirit;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            var SubType = evnt.BCard.SubType;

            if (target.Character != null)
            {
                if (SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP ||
                    SubType == (byte)AdditionalTypes.BearSpirit.DecreaseMaximumHP)
                {
                    target.Character.HPLoad();

                    if (target.Character.Session != null)
                    {
                        target.Character.Session.SendPacket(target.Character.GenerateStat());
                    }
                }
                else if (SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP ||
                    SubType == (byte)AdditionalTypes.BearSpirit.DecreaseMaximumMP)
                {
                    target.Character.MPLoad();

                    if (target.Character.Session != null)
                    {
                        target.Character.Session.SendPacket(target.Character.GenerateStat());
                    }
                }
            }
        }
    }
}
