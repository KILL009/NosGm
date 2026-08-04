using System.Collections.Generic;
using System.Linq;
using NosGm.Domain;

namespace NosGm.GameObject
{
    public class EventContainer
    {
        #region Instantiation

        public EventContainer(MapInstance mapInstance, EventActionType eventActionType, object param)
        {
            MapInstance = mapInstance;
            EventActionType = eventActionType;

            // EventHelper historically consumes SPAWNMONSTERS as a concrete List.
            // Instant Battle supplied a ConcurrentBag, which compiled because the
            // parameter is object but failed at runtime on the first wave with an
            // InvalidCastException. Normalize any enumerable at the boundary so all
            // event producers share the same stable contract.
            if (eventActionType == EventActionType.SPAWNMONSTERS &&
                param is IEnumerable<MonsterToSummon> monsters &&
                !(param is List<MonsterToSummon>))
            {
                Parameter = monsters.ToList();
            }
            else
            {
                Parameter = param;
            }
        }

        #endregion

        #region Properties

        public EventActionType EventActionType { get; set; }

        public MapInstance MapInstance { get; set; }

        public object Parameter { get; set; }

        #endregion
    }
}
