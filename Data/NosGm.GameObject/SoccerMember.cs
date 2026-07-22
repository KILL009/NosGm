using NosGm.Domain;

namespace NosGm.GameObject
{
    public class SoccerMember
    {
        public ClientSession Session { get; set; }
        public long? GroupId { get; set; }
        public EventType SOCCER { get; set; }
    }
}
