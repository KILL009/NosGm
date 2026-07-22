using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class AbsorbedSpiritHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.AbsorbedSpirit;

        public void Execute(BCardEvent evnt)
        {
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var SubType = evnt.BCard.SubType;

            bool hasSpiritAbsorption = target.HasBuff(596);

            if (SubType == (byte)AdditionalTypes.AbsorbedSpirit.ApplyEffectIfPresent)
            {
                if (hasSpiritAbsorption)
                {
                    target.AddBuff(new Buff((short)SecondData, target.Level), target);
                    target.RemoveBuff(596);
                }
            }
            if (SubType == (byte)AdditionalTypes.AbsorbedSpirit.ApplyEffectIfNotPresent)
            {
                if (!hasSpiritAbsorption && !target.HasBuff(599))
                {
                    target.AddBuff(new Buff((short)SecondData, target.Level), target);
                }
            }
        }
    }
}
