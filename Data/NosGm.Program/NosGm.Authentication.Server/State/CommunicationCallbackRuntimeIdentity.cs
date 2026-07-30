namespace NosGm.Authentication.Server.State;

public sealed class CommunicationCallbackRuntimeIdentity
{
    public CommunicationCallbackRuntimeIdentity(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        GenerationId = Guid.NewGuid();
        StartedAt = timeProvider.GetUtcNow();
    }

    public Guid GenerationId { get; }

    public DateTimeOffset StartedAt { get; }
}
