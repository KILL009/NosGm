using System;
using System.Collections.Generic;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Communication
{
    public sealed class CommunicationCallbackSubscriptionOptions
    {
        public CommunicationCallbackSubscriptionOptions(
            AuthenticationGrpcClientOptions transport,
            string worldId = null,
            int channelId = 0,
            string worldGroup = null,
            IEnumerable<WireV1.CommunicationCallbackKind> acceptedKinds = null)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            if (transport.CallerRole != ClusterNodeRole.Login &&
                transport.CallerRole != ClusterNodeRole.World)
            {
                throw new InvalidOperationException(
                    "Communication callback subscriptions require the Login or World role.");
            }

            if (transport.CallerRole == ClusterNodeRole.World)
            {
                if (!Guid.TryParse(worldId, out _) ||
                    channelId <= 0 || channelId > 51 ||
                    string.IsNullOrWhiteSpace(worldGroup) ||
                    worldGroup.Length > 64 ||
                    !string.Equals(worldGroup, worldGroup.Trim(), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "World callback subscriptions require a canonical World ID, channel, and bounded group.");
                }
            }
            else if (!string.IsNullOrEmpty(worldId) || channelId != 0 ||
                     !string.IsNullOrEmpty(worldGroup))
            {
                throw new InvalidOperationException(
                    "Login callback subscriptions cannot claim a World identity.");
            }

            WorldId = worldId ?? string.Empty;
            ChannelId = channelId;
            WorldGroup = worldGroup ?? string.Empty;
            AcceptedKinds = acceptedKinds == null
                ? Array.Empty<WireV1.CommunicationCallbackKind>()
                : new List<WireV1.CommunicationCallbackKind>(acceptedKinds).AsReadOnly();
        }

        public AuthenticationGrpcClientOptions Transport { get; }
        public string WorldId { get; }
        public int ChannelId { get; }
        public string WorldGroup { get; }
        public IReadOnlyCollection<WireV1.CommunicationCallbackKind> AcceptedKinds { get; }
    }
}
