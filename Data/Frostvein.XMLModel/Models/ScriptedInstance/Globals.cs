using Frostvein.XMLModel.Objects;
using System;

namespace Frostvein.XMLModel.Models.ScriptedInstance
{
    [Serializable]
    public class Globals
    {
        #region Properties

        public DailyEntries DailyEntries { get; set; }

        public Item[] DrawItems { get; set; }

        public FamExp FamExp { get; set; }

        public FamMission FamMission { get; set; }

        public GiantTeam GiantTeam { get; set; }

        public MediumTeam MediumTeam { get; set; }

        public Team Team { get; set; }

        public Item[] GiftItems { get; set; }

        public PetsRewards PetsRewards { get; set; }

        public Gold Gold { get; set; }

        public Id Id { get; set; }

        public Bool IsIndividual { get; set; }

        public Label Label { get; set; }

        public Level LevelMaximum { get; set; }

        public Level LevelMinimum { get; set; }

        public Lives Lives { get; set; }

        public Name Name { get; set; }

        public Reputation Reputation { get; set; }

        public Item[] RequiredItems { get; set; }

        public Item[] SpecialItems { get; set; }

        public SpNeeded SpNeeded { get; set; }

        public StartPosition StartX { get; set; }

        public StartPosition StartY { get; set; }

        #endregion
    }
}