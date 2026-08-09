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
    private readonly ConfigurationRuntimeController _runtimeController;
    private readonly TimeProvider _timeProvider;

    public ClusterConfigurationService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        ClientCertificateRoleMap roleMap,
        ConfigurationRuntimeController runtimeController,
        TimeProvider timeProvider,
        ILogger<ClusterConfigurationService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _roleMap = roleMap;
        _runtimeController = runtimeController;
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
                    context,
                    requireGrpcDeadline: true,
                    WireV1.ClusterNodeRole.World,
                    WireV1.ClusterNodeRole.Master);
                if (validation != WireV1.ConfigurationResultCode.Success)
                {
                    WriteAudit(request?.Context, "GetConfiguration", validation, 0);
                    return Task.FromResult(
                        new WireV1.GetConfigurationResponse
                        {
                            Result = validation
                        });
                }

                if (!_runtimeController.TryGet(
                        out ClusterConfigurationState.SnapshotState state,
                        out Guid runtimeGenerationId))
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
                        RuntimeGenerationId =
                            runtimeGenerationId.ToString("D")
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
                    context,
                    requireGrpcDeadline: true,
                    WireV1.ClusterNodeRole.World,
                    WireV1.ClusterNodeRole.Master);
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
                    _runtimeController.Update(
                        request.Configuration,
                        out Guid runtimeGenerationId);
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
                        RuntimeGenerationId =
                            runtimeGenerationId.ToString("D")
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.GetConfigurationRuntimeInfoResponse>
        GetConfigurationRuntimeInfo(
            WireV1.GetConfigurationRuntimeInfoRequest request,
            ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.ConfigurationResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        ClusterConfigurationContractValidator.Validate(request),
                        "GetConfigurationRuntimeInfo",
                        context,
                        requireGrpcDeadline: true,
                        WireV1.ClusterNodeRole.Master);
                if (validation != WireV1.ConfigurationResultCode.Success)
                {
                    return Task.FromResult(
                        new WireV1.GetConfigurationRuntimeInfoResponse
                        {
                            Result = validation
                        });
                }

                ConfigurationRuntimeStatus status =
                    _runtimeController.GetStatus();
                WriteAudit(
                    request.Context,
                    "GetConfigurationRuntimeInfo",
                    WireV1.ConfigurationResultCode.Success,
                    status.ConfigurationGeneration);
                return Task.FromResult(ToRuntimeInfoResponse(status));
            },
            context.CancellationToken);
    }

    public override Task<WireV1.RestartConfigurationRuntimeResponse>
        RestartConfigurationRuntime(
            WireV1.RestartConfigurationRuntimeRequest request,
            ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.ConfigurationResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        ClusterConfigurationContractValidator.Validate(request),
                        "RestartConfigurationRuntime",
                        context,
                        requireGrpcDeadline: true,
                        WireV1.ClusterNodeRole.Master);
                if (validation != WireV1.ConfigurationResultCode.Success)
                {
                    return Task.FromResult(
                        new WireV1.RestartConfigurationRuntimeResponse
                        {
                            Result = validation
                        });
                }

                Guid previousRuntimeGenerationId =
                    _runtimeController.GetStatus().RuntimeGenerationId;
                Guid expectedRuntimeGenerationId = Guid.ParseExact(
                    request.ExpectedRuntimeGenerationId,
                    "D");
                ConfigurationRuntimeRestartResult restartResult =
                    _runtimeController.TryRestart(
                        expectedRuntimeGenerationId,
                        out ConfigurationRuntimeStatus status);
                WireV1.ConfigurationResultCode result = restartResult switch
                {
                    ConfigurationRuntimeRestartResult.Success =>
                        WireV1.ConfigurationResultCode.Success,
                    ConfigurationRuntimeRestartResult
                            .RuntimeGenerationChanged =>
                        WireV1.ConfigurationResultCode.Conflict,
                    _ => WireV1.ConfigurationResultCode.Unavailable
                };
                WriteAudit(
                    request.Context,
                    "RestartConfigurationRuntime",
                    result,
                    status.ConfigurationGeneration);
                _logger.LogInformation(
                    "Configuration runtime restart completed with {Result}; previous generation {PreviousRuntimeGenerationId}; current generation {RuntimeGenerationId}; restart count {RestartCount}; active subscribers {ActiveSubscribers}.",
                    result,
                    previousRuntimeGenerationId,
                    status.RuntimeGenerationId,
                    status.RestartCount,
                    status.ActiveSubscriptions);
                return Task.FromResult(
                    ToRuntimeRestartResponse(
                        result,
                        previousRuntimeGenerationId,
                        status));
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
            requireGrpcDeadline: false,
            WireV1.ClusterNodeRole.World);
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
        ConfigurationSubscriptionOpenResult openResult =
            _runtimeController.TryOpenSubscription(
                Guid.ParseExact(request.RuntimeGenerationId, "D"),
                request.Context.CallerInstanceId,
                request.ResumeAfterGeneration,
                out ClusterConfigurationSubscription subscription,
                out Guid runtimeGenerationId);
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
                    await WriteEnvelopeAsync(
                        responseStream,
                        ToEnvelope(
                            replay,
                            replayed: true,
                            runtimeGenerationId),
                        linked.Token);
                }

                await foreach (ClusterConfigurationState.SnapshotState update in
                               subscription.PendingUpdates.ReadAllAsync(linked.Token))
                {
                    await WriteEnvelopeAsync(
                        responseStream,
                        ToEnvelope(
                            update,
                            replayed: false,
                            runtimeGenerationId),
                        linked.Token);
                }

                ThrowForSubscriptionTermination(
                    subscription.TerminationReason);
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
            catch (OperationCanceledException)
                when (subscription.TerminationReason ==
                      ConfigurationSubscriptionTerminationReason
                          .RuntimeRestarted)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration runtime restarted."));
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
        bool requireGrpcDeadline,
        params WireV1.ClusterNodeRole[] allowedRoles)
    {
        var certificate =
            callContext.GetHttpContext().Connection.ClientCertificate;
        if (!_roleMap.TryResolveRole(
                certificate,
                out WireV1.ClusterNodeRole certificateRole) ||
            !allowedRoles.Contains(certificateRole))
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

    private static bool IsCanonicalRuntimeGeneration(string value)
    {
        return Guid.TryParseExact(value, "D", out Guid parsed) &&
               parsed != Guid.Empty &&
               string.Equals(
                   value,
                   parsed.ToString("D"),
                   StringComparison.Ordinal);
    }

    private WireV1.ConfigurationUpdateEnvelope ToEnvelope(
        ClusterConfigurationState.SnapshotState state,
        bool replayed,
        Guid runtimeGenerationId)
    {
        return new WireV1.ConfigurationUpdateEnvelope
        {
            Configuration = state.Configuration,
            Generation = state.Generation,
            RuntimeGenerationId = runtimeGenerationId.ToString("D"),
            Replayed = replayed
        };
    }

    private static Task WriteEnvelopeAsync(
        IServerStreamWriter<WireV1.ConfigurationUpdateEnvelope> writer,
        WireV1.ConfigurationUpdateEnvelope envelope,
        CancellationToken cancellationToken)
    {
        return writer.WriteAsync(envelope, cancellationToken);
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
                        "The Configuration authority has not been seeded."));
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
            case ConfigurationSubscriptionOpenResult.RuntimeChanged:
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration runtime generation changed before the stream opened."));
            default:
                throw new RpcException(
                    new Status(
                        StatusCode.Internal,
                        "The Configuration subscription result is unknown."));
        }
    }

    private static void ThrowForSubscriptionTermination(
        ConfigurationSubscriptionTerminationReason reason)
    {
        switch (reason)
        {
            case ConfigurationSubscriptionTerminationReason.None:
                return;
            case ConfigurationSubscriptionTerminationReason.QueueOverflow:
                throw new RpcException(
                    new Status(
                        StatusCode.ResourceExhausted,
                        "The Configuration subscriber fell behind its bounded queue."));
            case ConfigurationSubscriptionTerminationReason.Superseded:
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration subscription was replaced by a newer connection."));
            case ConfigurationSubscriptionTerminationReason.RuntimeRestarted:
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The Configuration runtime restarted."));
            default:
                throw new RpcException(
                    new Status(
                        StatusCode.Internal,
                        "The Configuration subscription termination is unknown."));
        }
    }

    private static WireV1.GetConfigurationRuntimeInfoResponse
        ToRuntimeInfoResponse(ConfigurationRuntimeStatus status)
    {
        return new WireV1.GetConfigurationRuntimeInfoResponse
        {
            Result = WireV1.ConfigurationResultCode.Success,
            RuntimeGenerationId = status.RuntimeGenerationId.ToString("D"),
            StartedAtUnixTimeMs =
                status.StartedAt.ToUnixTimeMilliseconds(),
            ConfigurationGeneration = status.ConfigurationGeneration,
            Seeded = status.Seeded,
            ActiveSubscribers = checked((uint)status.ActiveSubscriptions),
            RestartCount = status.RestartCount,
            ControlEnabled = status.ControlEnabled
        };
    }

    private static WireV1.RestartConfigurationRuntimeResponse
        ToRuntimeRestartResponse(
            WireV1.ConfigurationResultCode result,
            Guid previousRuntimeGenerationId,
            ConfigurationRuntimeStatus status)
    {
        return new WireV1.RestartConfigurationRuntimeResponse
        {
            Result = result,
            PreviousRuntimeGenerationId =
                previousRuntimeGenerationId.ToString("D"),
            RuntimeGenerationId = status.RuntimeGenerationId.ToString("D"),
            StartedAtUnixTimeMs =
                status.StartedAt.ToUnixTimeMilliseconds(),
            ConfigurationGeneration = status.ConfigurationGeneration,
            ActiveSubscribers = checked((uint)status.ActiveSubscriptions),
            RestartCount = status.RestartCount,
            ControlEnabled = status.ControlEnabled
        };
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
