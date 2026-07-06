using Frostvein.Domain;

namespace Frostvein.GameObject
{
    public class ArenaMember
    {
        #region Properties

        public EventType ArenaType { get; set; }

        public long? GroupId { get; set; }

        public ClientSession Session { get; set; }

        public short Time { get; set; }

        #endregion
    }
}