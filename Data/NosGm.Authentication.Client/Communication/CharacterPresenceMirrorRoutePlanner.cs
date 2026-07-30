using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackWorldRoute
    {
        public Guid WorldId { get; set; }

        public string WorldGroup { get; set; }
    }

    public static class CharacterPresenceMirrorRoutePlanner
    {
        public static IReadOnlyList<Guid> ResolvePeerWorldIds(
            IEnumerable<CommunicationCallbackWorldRoute> worlds,
            Guid sourceWorldId)
        {
            if (worlds == null)
            {
                throw new ArgumentNullException(nameof(worlds));
            }
            if (sourceWorldId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The source World ID cannot be empty.",
                    nameof(sourceWorldId));
            }

            CommunicationCallbackWorldRoute[] snapshot = worlds
                .Where(route => route != null)
                .ToArray();
            CommunicationCallbackWorldRoute source = snapshot
                .FirstOrDefault(route => route.WorldId == sourceWorldId);
            if (source == null || string.IsNullOrWhiteSpace(source.WorldGroup))
            {
                return Array.Empty<Guid>();
            }

            return snapshot
                .Where(route =>
                    route.WorldId != Guid.Empty &&
                    route.WorldId != sourceWorldId &&
                    string.Equals(
                        route.WorldGroup,
                        source.WorldGroup,
                        StringComparison.Ordinal))
                .Select(route => route.WorldId)
                .Distinct()
                .OrderBy(worldId => worldId)
                .ToArray();
        }
    }
}
