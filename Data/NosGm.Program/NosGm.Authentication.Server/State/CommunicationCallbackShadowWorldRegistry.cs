using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public sealed class CommunicationCallbackShadowWorldRegistry
{
    private sealed class Registration
    {
        public required Guid WorldId { get; init; }
        public required int ChannelId { get; init; }
        public required string WorldGroup { get; init; }
        public required string CallerInstanceId { get; init; }
        public required string RuntimeGenerationId { get; init; }
    }

    private readonly CommunicationCallbackHub _hub;
    private readonly Dictionary<Guid, Registration> _registrations = new();
    private readonly object _syncRoot = new();

    public CommunicationCallbackShadowWorldRegistry(
        CommunicationCallbackHub hub)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    }

    public WireV1.CommunicationResultCode Register(
        WireV1.RegisterCommunicationCallbackShadowWorldRequest request)
    {
        Guid worldId = Guid.ParseExact(request.WorldId, "D");
        lock (_syncRoot)
        {
            if (_registrations.TryGetValue(
                    worldId,
                    out Registration existing))
            {
                return Matches(existing, request)
                    ? WireV1.CommunicationResultCode.Success
                    : WireV1.CommunicationResultCode.Conflict;
            }

            WireV1.CommunicationResultCode result = _hub.RegisterWorld(
                worldId,
                request.ChannelId,
                request.WorldGroup);
            if (result == WireV1.CommunicationResultCode.Success)
            {
                _registrations.Add(
                    worldId,
                    new Registration
                    {
                        WorldId = worldId,
                        ChannelId = request.ChannelId,
                        WorldGroup = request.WorldGroup,
                        CallerInstanceId = request.Context.CallerInstanceId,
                        RuntimeGenerationId = request.RuntimeGenerationId
                    });
            }
            return result;
        }
    }

    public WireV1.CommunicationResultCode Unregister(
        WireV1.UnregisterCommunicationCallbackShadowWorldRequest request)
    {
        Guid worldId = Guid.ParseExact(request.WorldId, "D");
        lock (_syncRoot)
        {
            if (!_registrations.TryGetValue(
                    worldId,
                    out Registration existing))
            {
                return WireV1.CommunicationResultCode.NotFound;
            }
            if (!string.Equals(
                    existing.CallerInstanceId,
                    request.Context.CallerInstanceId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    existing.RuntimeGenerationId,
                    request.RuntimeGenerationId,
                    StringComparison.Ordinal))
            {
                return WireV1.CommunicationResultCode.Conflict;
            }

            _registrations.Remove(worldId);
            _hub.UnregisterWorld(worldId);
            return WireV1.CommunicationResultCode.Success;
        }
    }

    public bool Owns(
        WireV1.SubscribeCommunicationCallbacksRequest request)
    {
        Guid worldId = Guid.ParseExact(request.WorldId, "D");
        lock (_syncRoot)
        {
            return _registrations.TryGetValue(
                       worldId,
                       out Registration registration) &&
                   registration.ChannelId == request.ChannelId &&
                   string.Equals(
                       registration.WorldGroup,
                       request.WorldGroup,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       registration.CallerInstanceId,
                       request.Context.CallerInstanceId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       registration.RuntimeGenerationId,
                       request.RuntimeGenerationId,
                       StringComparison.Ordinal);
        }
    }

    private static bool Matches(
        Registration existing,
        WireV1.RegisterCommunicationCallbackShadowWorldRequest request)
    {
        return existing.ChannelId == request.ChannelId &&
               string.Equals(
                   existing.WorldGroup,
                   request.WorldGroup,
                   StringComparison.Ordinal) &&
               string.Equals(
                   existing.CallerInstanceId,
                   request.Context.CallerInstanceId,
                   StringComparison.Ordinal) &&
               string.Equals(
                   existing.RuntimeGenerationId,
                   request.RuntimeGenerationId,
                   StringComparison.Ordinal);
    }
}
