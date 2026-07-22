using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class Card : CardDTO
    {
        #region Properties

        public List<BCard> BCards { get; set; }

        #endregion

        #region Instantiation

        public Card()
        {
        }

        public Card(CardDTO input)
        {
            BuffType = input.BuffType;
            CardId = input.CardId;
            Delay = input.Delay;
            Duration = input.Duration;
            EffectId = input.EffectId;
            Level = input.Level;
            Name = input.Name;
            Propability = input.Propability;
            TimeoutBuff = input.TimeoutBuff;
            TimeoutBuffChance = input.TimeoutBuffChance;
        }

        #endregion
    }
}