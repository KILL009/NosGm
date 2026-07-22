using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class CardMapper
    {
        #region Methods

        public static bool ToCard(CardDTO input, Card output)
        {
            if (input == null) return false;

            output.BuffType = input.BuffType;
            output.CardId = input.CardId;
            output.Delay = input.Delay;
            output.Duration = input.Duration;
            output.EffectId = input.EffectId;
            output.Level = input.Level;
            output.Name = input.Name;
            output.Propability = input.Propability;
            output.TimeoutBuff = input.TimeoutBuff;
            output.TimeoutBuffChance = input.TimeoutBuffChance;

            return true;
        }

        public static bool ToCardDTO(Card input, CardDTO output)
        {
            if (input == null) return false;

            output.BuffType = input.BuffType;
            output.CardId = input.CardId;
            output.Delay = input.Delay;
            output.Duration = input.Duration;
            output.EffectId = input.EffectId;
            output.Level = input.Level;
            output.Name = input.Name;
            output.Propability = input.Propability;
            output.TimeoutBuff = input.TimeoutBuff;
            output.TimeoutBuffChance = input.TimeoutBuffChance;

            return true;
        }

        #endregion
    }
}