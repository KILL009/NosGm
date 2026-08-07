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
    private readonly ClusterConfigurationState _state;
    private readonly TimeProvider _timeProvider;

    public ClusterConfigurationService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        ClientCertificateRoleMap roleMap,
        ClusterConfigurationState state,
        TimeProvider timeProvider,
        ILogger<ClusterConfigurationService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _roleMap = roleMap;
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
                        Generation = state.Generation
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
                        Generation = state.Generation
                    });
            },
            context.CancellationToken);
    }

    private WireV1.ConfigurationResultCode ValidateAndAuthorize(
        WireV1.RequestContext requestContext,
        ConfigurationContractValidationError validationError,
        string operation,
        ServerCallContext callContext)
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

        return _replayGuard.TryAccept(
            requestContext.RequestId,
            requestDeadline,
            now)
            ? WireV1.ConfigurationResultCode.Success
            : WireV1.ConfigurationResultCode.Conflict;
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
