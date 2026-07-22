using NosGm.Domain;

namespace NosGm.GameObject
{
    public class SoccerTeamMember
    {
        public SoccerTeamMember(ClientSession session, SoccerTeamType soccerTeamType)
        {
            Session = session;
            SoccerTeamType = soccerTeamType;
        }

        public ClientSession Session { get; set; }
        public SoccerTeamType SoccerTeamType { get; set; }
    }
}
