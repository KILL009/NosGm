using Frostvein.Domain;

namespace Frostvein.GameObject
{
    public class SoccerMember
    {
        public ClientSession Session { get; set; }
        public long? GroupId { get; set; }
        public EventType SOCCER { get; set; }
    }
}
