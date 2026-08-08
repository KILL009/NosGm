using Grpc.Core;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.Configuration.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.Services;

public sealed class ClusterConfigurationService
    : WireV1.ClusterConfiguration.ClusterConfigurationBase
{
    private readonly AuthenticationDispatchGate _dispatchGate;
    private readonly ILogger<ClusterConfigurationService> _logger;
    private readonly AuthenticationRequestReplayGuard _replayGuard;
    private readonly ClientCertificateRoleMap _roleMap;
    private readonly CommunicationCallbackRuntimeIdentity _runtimeIdentity;
    private readonly ClusterConfigurationState _state;
    private readonly TimeProvider _timeProvider;

    public ClusterConfigurationService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        ClientCertificateRoleMap roleMap,
        CommunicationCallbackRuntimeIdentity runtimeIdentity,
        ClusterConfigurationState state,
        TimeProvider timeProvider,
        ILogger<ClusterConfigurationService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _roleMap = roleMap;
        _runtimeIdentity = runtimeIdentity;
        _state = state;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override Task<WireV1.GetConfigurationResponse> GetConfiguration(
        WireV1.GetConfigurationRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.ConfigurationResultCode validation = ValidateAndAuthorize(
                    request?.Context,
                    ClusterConfigurationContractValidator.Validate(request),
                    "GetConfiguration",
                    context);
                if (validation != WireV1.ConfigurationResultCode.Success)
                {
                    WriteAudit(request?.Context, "GetConfiguration", validation, 0);
                    return Task.FromResult(
                        new WireV1.GetConfigurationResponse
                        {
                            Result = validation
                        });
                }

                if (!_state.TryGet(out ClusterConfigurationState.SnapshotState state))
                {
                    WriteAudit(
                        request.Context,
                        "GetConfiguration",
                        WireV1.ConfigurationResultCode.Unavailable,
                        0);
                    return Task.FromResult(
                        new WireV1.GetConfigurationResponse
                        {
                            Result = WireV1.ConfigurationResultCode.Unavailable,
                            Generation = 0
                        });
                }

                WriteAudit(
                    request.Context,
                    "GetConfiguration",
                    WireV1.ConfigurationResultCode.Success,
                    state.Generation);
                return Task.FromResult(
                    new WireV1.GetConfigurationResponse
                    {
                        Result = WireV1.ConfigurationResultCode.Success,
                        Configuration = state.Configuration,
                        Generation = state.Generation,
                        RuntimeGenerationId = RuntimeGenerationId
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.UpdateConfigurationResponse> UpdateConfiguration(
        WireV1.UpdateConfigurationRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.ConfigurationResultCode validation = ValidateAndAuthorize(
                    request?.Context,
                    ClusterConfigurationContractValidator.Validate(request),
                    "UpdateConfiguration",
                    context);
                if (validation != WireV1.ConfigurationResultCode.Success)
                {
                    WriteAudit(request?.Context, "UpdateConfiguration", validation, 0);
                    return Task.FromResult(
                        new WireV1.UpdateConfigurationResponse
                        {
                            Result = validation
                        });
                }

                ClusterConfigurationState.SnapshotState state =
                    _state.Update(request.Configuration);
                WriteAudit(
                    request.Context,
                    "UpdateConfiguration",
                    WireV1.ConfigurationResultCode.Success,
                    state.Generation);
                return Task.FromResult(
                    new WireV1.UpdateConfigurationResponse
                    {
                        Result = WireV1.ConfigurationResultCode.Success,
                        Configuration = state.Configuration,
                        Generation = state.Generation,
                        RuntimeGenerationId = RuntimeGenerationId
                    });
            },
            context.CancellationToken);
    }

    public override async Task SubscribeConfigurationUpdates(
        WireV1.SubscribeConfigurationUpdatesRequest request,
        IServerStreamWriter<WireV1.ConfigurationUpdateEnvelope> responseStream,
        ServerCallContext context)
    {
        WireV1.ConfigurationResultCode authorization = ValidateAndAuthorize(
            request?.Context,
            ClusterConfigurationContractValidator.Validate(request),
            "SubscribeConfigurationUpdates",
            context,
            requireGrpcDeadline: false);
        if (authorization != WireV1.ConfigurationResultCode.Success)
        {
            throw new RpcException(
                new Status(
                    authorization == WireV1.ConfigurationResultCode.Conflict
                        ? StatusCode.AlreadyExists
                        : StatusCode.InvalidArgument,
                    "The Configuration update subscription was rejected."));
        }
        if (!IsCanonicalRuntimeGeneration(request.RuntimeGenerationId))
        {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    "The Configuration runtime generation is invalid."));
        }
        if (!string.Equals(
                request.RuntimeGenerationId,
                RuntimeGenerationId,
                StringComparison.Ordinal))
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "The Configuration runtime generation changed before the stream opened."));
        }

        ConfigurationSubscriptionOpenResult openResult =
            _state.TryOpenSubscription(
                request.Context.CallerInstanceId,
                request.ResumeAfterGeneration,
                out ClusterConfigurationSubscription subscription);
        ThrowForSubscriptionOpenResult(openResult);
        WriteAudit(
            request.Context,
            "SubscribeConfigurationUpdates",
            WireV1.ConfigurationResultCode.Success,
            request.ResumeAfterGeneration);

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken,
                subscription.TerminationToken);
            try
            {
                foreach (ClusterConfigurationState.SnapshotState replay in
                         subscription.ReplayUpdates)
                {
                    linked.Token.ThrowIfCancellationRequested();
                    await responseStream.WriteAsync(
                        ToEnvelope(replay, replayed: true));
                }

                await foreach (ClusterConfigurationState.SnapshotState update in
                               subscription.PendingUpdates.ReadAllAsync(linked.Token))
                {
                    await responseStream.WriteAsync(
                        ToEnvelope(update, replayed: false));
                }
            }
            catch (OperationCanceledException)
                when (context.CancellationToken.IsCancellationRequested)
            {
                // Normal client disconnect or server shutdown.
            }
            catch (OperationCanceledException)
                when (subscription.TerminationReason ==
                      ConfigurationSubscriptionTerminationReason.QueueOverflow)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.ResourceExhausted,
                        "The Configuration subscriber fell behind its bounded queue."));
            }
            catch (OperationCanceledException)
                when (subscription.TerminationReason ==
                      ConfigurationSubscriptionTerminationReason.Superseded)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration subscription was replaced by a newer connection."));
            }
        }
        finally
        {
            await subscription.DisposeAsync();
        }
    }

    private WireV1.ConfigurationResultCode ValidateAndAuthorize(
        WireV1.RequestContext requestContext,
        ConfigurationContractValidationError validationError,
        string operation,
        ServerCallContext callContext,
        bool requireGrpcDeadline = true)
    {
        var certificate =
            callContext.GetHttpContext().Connection.ClientCertificate;
        if (!_roleMap.TryResolveRole(
                certificate,
                out WireV1.ClusterNodeRole certificateRole) ||
            certificateRole != WireV1.ClusterNodeRole.World)
        {
            _logger.LogWarning(
                "Configuration RPC {Operation} rejected an unauthorized client certificate.",
                operation);
            throw new RpcException(
                new Status(
                    StatusCode.PermissionDenied,
                    "The client certificate is not authorized for this operation."));
        }

        if (validationError != ConfigurationContractValidationError.None ||
            requestContext == null ||
            requestContext.CallerRole != certificateRole)
        {
            return WireV1.ConfigurationResultCode.InvalidRequest;
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        long issuedAt = requestContext.IssuedAtUnixTimeMs;
        long requestDeadline = requestContext.DeadlineUnixTimeMs;
        if (issuedAt < now - ClusterProtocolLimits.MaxClockSkewMilliseconds ||
            issuedAt > now + ClusterProtocolLimits.MaxClockSkewMilliseconds ||
            requestDeadline <= now ||
            requestDeadline > now + ClusterProtocolLimits.MaxDeadlineMilliseconds)
        {
            return WireV1.ConfigurationResultCode.InvalidRequest;
        }

        if (requireGrpcDeadline)
        {
            DateTime grpcDeadline = callContext.Deadline;
            if (grpcDeadline == DateTime.MaxValue)
            {
                return WireV1.ConfigurationResultCode.InvalidRequest;
            }

            long grpcDeadlineMilliseconds =
                new DateTimeOffset(grpcDeadline.ToUniversalTime())
                    .ToUnixTimeMilliseconds();
            if (grpcDeadlineMilliseconds <= now ||
                grpcDeadlineMilliseconds >
                now +
                ClusterProtocolLimits.MaxDeadlineMilliseconds +
                ClusterProtocolLimits.MaxClockSkewMilliseconds ||
                requestDeadline >
                grpcDeadlineMilliseconds +
                ClusterProtocolLimits.MaxClockSkewMilliseconds)
            {
                return WireV1.ConfigurationResultCode.InvalidRequest;
            }
        }

        return _replayGuard.TryAccept(
            requestContext.RequestId,
            requestDeadline,
            now)
            ? WireV1.ConfigurationResultCode.Success
            : WireV1.ConfigurationResultCode.Conflict;
    }

    private string RuntimeGenerationId =>
        _runtimeIdentity.GenerationId.ToString("D");

    private static bool IsCanonicalRuntimeGeneration(string value)
    {
        return Guid.TryParseExact(value, "D", out Guid parsed) &&
               string.Equals(
                   value,
                   parsed.ToString("D"),
                   StringComparison.Ordinal);
    }

    private WireV1.ConfigurationUpdateEnvelope ToEnvelope(
        ClusterConfigurationState.SnapshotState state,
        bool replayed)
    {
        return new WireV1.ConfigurationUpdateEnvelope
        {
            Configuration = state.Configuration,
            Generation = state.Generation,
            RuntimeGenerationId = RuntimeGenerationId,
            Replayed = replayed
        };
    }

    private static void ThrowForSubscriptionOpenResult(
        ConfigurationSubscriptionOpenResult result)
    {
        switch (result)
        {
            case ConfigurationSubscriptionOpenResult.Success:
                return;
            case ConfigurationSubscriptionOpenResult.Unavailable:
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration shadow state has not been seeded."));
            case ConfigurationSubscriptionOpenResult.InvalidResumeCursor:
                throw new RpcException(
                    new Status(
                        StatusCode.OutOfRange,
                        "The Configuration resume generation is no longer retained."));
            case ConfigurationSubscriptionOpenResult.CapacityExceeded:
                throw new RpcException(
                    new Status(
                        StatusCode.ResourceExhausted,
                        "The Configuration subscriber capacity was reached."));
            default:
                throw new RpcException(
                    new Status(
                        StatusCode.Internal,
                        "The Configuration subscription result is unknown."));
        }
    }

    private void WriteAudit(
        WireV1.RequestContext requestContext,
        string operation,
        WireV1.ConfigurationResultCode result,
        ulong generation)
    {
        string requestId =
            requestContext != null &&
            Guid.TryParseExact(
                requestContext.RequestId,
                "D",
                out Guid parsedRequestId)
                ? parsedRequestId.ToString("D")
                : "invalid";
        _logger.LogInformation(
            "Configuration RPC {Operation} completed with {Result}; request {RequestId}; generation {Generation}.",
            operation,
            result,
            requestId,
            generation);
    }
}
