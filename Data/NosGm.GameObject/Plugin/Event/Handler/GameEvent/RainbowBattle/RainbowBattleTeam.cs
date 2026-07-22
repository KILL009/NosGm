using NosGm.Domain;
using System;
using System.Collections.Generic;

namespace NosGm.GameObject.Plugin.Event
{
    public class RainbowBattleTeam
    {
        #region Instantiation

        public RainbowBattleTeam(List<ClientSession> session, RainbowTeamBattleType RmbTeamType, RainbowBattleTeam secondTeam)
        {
            Session = session;
            TeamEntity = RmbTeamType;
            SecondTeam = secondTeam;
            TotalFlag = new List<Tuple<int, RainbowNpcType>>();
        }

        #endregion

        #region Properties

        public long Score { get; set; }

        public RainbowBattleTeam SecondTeam { get; set; }

        public List<ClientSession> Session { get; set; }

        public RainbowTeamBattleType TeamEntity { get; set; }

        public List<Tuple<int, RainbowNpcType>> TotalFlag { get; set; }

        #endregion
    }
}