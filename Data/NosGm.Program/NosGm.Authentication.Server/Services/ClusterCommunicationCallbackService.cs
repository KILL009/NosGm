using Grpc.Core;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.Services;

public sealed class ClusterCommunicationCallbackService
    : WireV1.ClusterCommunicationCallbacks
        .ClusterCommunicationCallbacksBase
{
    private readonly AuthenticationDispatchGate _dispatchGate;
    private readonly CommunicationCallbackHub _hub;
    private readonly ILogger<ClusterCommunicationCallbackService> _logger;
    private readonly AuthenticationRequestReplayGuard _replayGuard;
    private readonly ClientCertificateRoleMap _roleMap;
    private readonly CommunicationCallbackRuntimeIdentity _runtimeIdentity;
    private readonly AuthenticationServerOptions _serverOptions;
    private readonly TimeProvider _timeProvider;

    public ClusterCommunicationCallbackService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        ClientCertificateRoleMap roleMap,
        CommunicationCallbackHub hub,
        CommunicationCallbackRuntimeIdentity runtimeIdentity,
        AuthenticationServerOptions serverOptions,
        TimeProvider timeProvider,
        ILogger<ClusterCommunicationCallbackService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _roleMap = roleMap;
        _hub = hub;
        _runtimeIdentity = runtimeIdentity;
        _serverOptions = serverOptions;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override Task<WireV1.GetCommunicationCallbackRuntimeInfoResponse>
        GetCommunicationCallbackRuntimeInfo(
            WireV1.GetCommunicationCallbackRuntimeInfoRequest request,
            ServerCallContext context)
    {
        WireV1.CommunicationResultCode authorization = ValidateAndAuthorize(
            request?.Context,
            CommunicationCallbackRuntimeInfoContractValidator.Validate(request),
            "GetCommunicationCallbackRuntimeInfo",
            context,
            requireGrpcDeadline: true,
            WireV1.ClusterNodeRole.Login,
            WireV1.ClusterNodeRole.World);
        WriteAudit(
            request?.Context,
            "GetCommunicationCallbackRuntimeInfo",
            authorization,
            _hub.CurrentSequence,
            matchedSubscribers: 0);
        if (authorization != WireV1.CommunicationResultCode.Success)
        {
            return Task.FromResult(
                new WireV1.GetCommunicationCallbackRuntimeInfoResponse
                {
                    Result = authorization
                });
        }

        return Task.FromResult(
            new WireV1.GetCommunicationCallbackRuntimeInfoResponse
            {
                Result = WireV1.CommunicationResultCode.Success,
                RuntimeGenerationId =
                    _runtimeIdentity.GenerationId.ToString("D"),
                StartedAtUnixTimeMs =
                    _runtimeIdentity.StartedAt.ToUnixTimeMilliseconds(),
                CurrentSequence = _hub.CurrentSequence
            });
    }

    public override async Task SubscribeCommunicationCallbacks(
        WireV1.SubscribeCommunicationCallbacksRequest request,
        IServerStreamWriter<WireV1.CommunicationCallbackEnvelope>
            responseStream,
        ServerCallContext context)
    {
        WireV1.CommunicationResultCode authorization = ValidateAndAuthorize(
            request?.Context,
            ClusterCommunicationCallbackContractValidator
                .ValidateSubscribe(request),
            "SubscribeCommunicationCallbacks",
            context,
            requireGrpcDeadline: false,
            WireV1.ClusterNodeRole.Login,
            WireV1.ClusterNodeRole.World);
        ThrowForSetupResult(
            authorization,
            "The callback subscription request was rejected.");

        if (!IsCanonicalGenerationId(request.RuntimeGenerationId))
        {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    "The callback runtime generation is invalid."));
        }
        if (!string.Equals(
                request.RuntimeGenerationId,
                _runtimeIdentity.GenerationId.ToString("D"),
                StringComparison.Ordinal))
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "The callback runtime generation changed before the stream opened."));
        }

        CallbackSubscriptionOpenResult openResult =
            _hub.TryOpenSubscription(request, out var subscription);
        if (openResult != CallbackSubscriptionOpenResult.Success)
        {
            ThrowForSubscriptionOpenResult(openResult);
        }

        WriteAudit(
            request.Context,
            "SubscribeCommunicationCallbacks",
            WireV1.CommunicationResultCode.Success,
            sequence: request.ResumeAfterSequence,
            matchedSubscribers: 1);
        try
        {
            foreach (WireV1.CommunicationCallbackEnvelope envelope in
                     subscription.ReplayEvents)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                if (!IsExpired(envelope))
                {
                    await responseStream.WriteAsync(envelope);
                }
            }

            using var linked = CancellationTokenSource
                .CreateLinkedTokenSource(
                    context.CancellationToken,
                    subscription.TerminationToken);
            try
            {
                await foreach (WireV1.CommunicationCallbackEnvelope envelope in
                               subscription.PendingEvents.ReadAllAsync(
                                   linked.Token))
                {
                    if (!IsExpired(envelope))
                    {
                        await responseStream.WriteAsync(envelope);
                    }
                }
            }
            catch (OperationCanceledException)
                when (context.CancellationToken.IsCancellationRequested)
            {
                // Normal client disconnect or server shutdown.
            }
            catch (OperationCanceledException)
                when (subscription.TerminationReason ==
                      CallbackSubscriptionTerminationReason.QueueOverflow)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.ResourceExhausted,
                        "The callback subscriber fell behind its bounded queue."));
            }
            catch (OperationCanceledException)
                when (subscription.TerminationReason ==
                      CallbackSubscriptionTerminationReason.WorldUnregistered)
            {
                throw new RpcException(
                    new Status(
                        StatusCode.FailedPrecondition,
                        "The subscribed World is no longer registered."));
            }
        }
        finally
        {
            await subscription.DisposeAsync();
        }
    }

    public override Task<WireV1.PublishCommunicationCallbackResponse>
        PublishCommunicationCallback(
            WireV1.PublishCommunicationCallbackRequest request,
            ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.CommunicationResultCode authorization =
                    ValidateAndAuthorize(
                        request?.Context,
                        ClusterCommunicationCallbackContractValidator
                            .ValidatePublish(request),
                        "PublishCommunicationCallback",
                        context,
                        requireGrpcDeadline: true,
                        WireV1.ClusterNodeRole.Master);
                if (authorization != WireV1.CommunicationResultCode.Success)
                {
                    WriteAudit(
                        request?.Context,
                        "PublishCommunicationCallback",
                        authorization,
                        sequence: 0,
                        matchedSubscribers: 0);
                    return Task.FromResult(
                        new WireV1.PublishCommunicationCallbackResponse
                        {
                            Result = authorization
                        });
                }

                if (!_serverOptions.AllowedFingerprints.TryGetValue(
                        WireV1.ClusterNodeRole.Master,
                        out IReadOnlyCollection<string> masterFingerprints) ||
                    masterFingerprints.Count == 0)
                {
                    throw new RpcException(
                        new Status(
                            StatusCode.FailedPrecondition,
                            "Master callback publication is not configured."));
                }

                CommunicationCallbackPublishResult result =
                    _hub.Publish(request);
                WriteAudit(
                    request.Context,
                    "PublishCommunicationCallback",
                    result.Result,
                    result.Sequence,
                    result.MatchedSubscribers);
                return Task.FromResult(
                    new WireV1.PublishCommunicationCallbackResponse
                    {
                        Result = result.Result,
                        AcceptedSequence = result.Sequence,
                        MatchedSubscribers = result.MatchedSubscribers
                    });
            },
            context.CancellationToken);
    }

    private WireV1.CommunicationResultCode ValidateAndAuthorize(
        WireV1.RequestContext requestContext,
        CommunicationCallbackContractValidationError validationError,
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
                "Communication callback RPC {Operation} rejected an unauthorized client certificate.",
                operation);
            throw new RpcException(
                new Status(
                    StatusCode.PermissionDenied,
                    "The client certificate is not authorized for this operation."));
        }

        if (validationError !=
                CommunicationCallbackContractValidationError.None ||
            requestContext == null ||
            requestContext.CallerRole != certificateRole)
        {
            return WireV1.CommunicationResultCode.InvalidRequest;
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        long issuedAt = requestContext.IssuedAtUnixTimeMs;
        long requestDeadline = requestContext.DeadlineUnixTimeMs;
        if (issuedAt < now - ClusterProtocolLimits.MaxClockSkewMilliseconds ||
            issuedAt > now + ClusterProtocolLimits.MaxClockSkewMilliseconds ||
            requestDeadline <= now ||
            requestDeadline >
            now + ClusterProtocolLimits.MaxDeadlineMilliseconds)
        {
            return WireV1.CommunicationResultCode.InvalidRequest;
        }

        if (requireGrpcDeadline)
        {
            DateTime grpcDeadline = callContext.Deadline;
            if (grpcDeadline == DateTime.MaxValue)
            {
                return WireV1.CommunicationResultCode.InvalidRequest;
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
                return WireV1.CommunicationResultCode.InvalidRequest;
            }
        }

        return _replayGuard.TryAccept(
            requestContext.RequestId,
            requestDeadline,
            now)
            ? WireV1.CommunicationResultCode.Success
            : WireV1.CommunicationResultCode.Conflict;
    }

    private bool IsExpired(WireV1.CommunicationCallbackEnvelope envelope)
    {
        return envelope.ExpiresAtUnixTimeMs <=
               _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
    }

    private static bool IsCanonicalGenerationId(string value)
    {
        return value != null &&
               value.Length == 36 &&
               Guid.TryParseExact(value, "D", out Guid parsed) &&
               parsed != Guid.Empty &&
               string.Equals(
                   parsed.ToString("D"),
                   value,
                   StringComparison.Ordinal);
    }

    private static void ThrowForSetupResult(
        WireV1.CommunicationResultCode result,
        string message)
    {
        if (result == WireV1.CommunicationResultCode.Success)
        {
            return;
        }

        throw new RpcException(
            new Status(
                result == WireV1.CommunicationResultCode.Conflict
                    ? StatusCode.AlreadyExists
                    : StatusCode.InvalidArgument,
                message));
    }

    private static void ThrowForSubscriptionOpenResult(
        CallbackSubscriptionOpenResult result)
    {
        Status status = result switch
        {
            CallbackSubscriptionOpenResult.InvalidResumeCursor =>
                new Status(
                    StatusCode.OutOfRange,
                    "The callback replay cursor is unavailable."),
            CallbackSubscriptionOpenResult.Conflict =>
                new Status(
                    StatusCode.AlreadyExists,
                    "The callback subscriber identity is already active or changed."),
            CallbackSubscriptionOpenResult.CapacityExceeded =>
                new Status(
                    StatusCode.ResourceExhausted,
                    "The callback subscriber registry is full."),
            CallbackSubscriptionOpenResult.NotFound =>
                new Status(
                    StatusCode.FailedPrecondition,
                    "The callback subscriber World is not registered."),
            _ => new Status(
                StatusCode.Internal,
                "The callback subscription could not be opened.")
        };
        throw new RpcException(status);
    }

    private void WriteAudit(
        WireV1.RequestContext requestContext,
        string operation,
        WireV1.CommunicationResultCode result,
        ulong sequence,
        uint matchedSubscribers)
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
            "Communication callback RPC {Operation} completed with {Result}; request {RequestId}; sequence {Sequence}; matched subscribers {MatchedSubscribers}.",
            operation,
            result,
            requestId,
            sequence,
            matchedSubscribers);
    }
}
