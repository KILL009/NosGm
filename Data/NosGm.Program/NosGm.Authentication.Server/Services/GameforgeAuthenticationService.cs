using Grpc.Core;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.Authentication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.Services;

public sealed class GameforgeAuthenticationService
    : WireV1.GameforgeAuthentication.GameforgeAuthenticationBase
{
    private readonly AuthenticationDispatchGate _dispatchGate;
    private readonly AuthenticationRequestReplayGuard _replayGuard;
    private readonly AuthenticationServerOptions _options;
    private readonly ClientCertificateRoleMap _roleMap;
    private readonly GameforgeAuthenticationState _state;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GameforgeAuthenticationService> _logger;

    public GameforgeAuthenticationService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        AuthenticationServerOptions options,
        ClientCertificateRoleMap roleMap,
        GameforgeAuthenticationState state,
        TimeProvider timeProvider,
        ILogger<GameforgeAuthenticationService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _options = options;
        _roleMap = roleMap;
        _state = state;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override Task<WireV1.IssueAuthTicketResponse> IssueAuthTicket(
        WireV1.IssueAuthTicketRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AuthenticationTransportResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        GameforgeAuthenticationContractValidator.Validate(
                            request),
                        WireV1.ClusterNodeRole.AuthBridge,
                        "IssueAuthTicket",
                        context);
                if (validation != AuthenticationTransportResultCode.Success)
                {
                    WriteAudit(
                        request?.Context,
                        WireV1.ClusterNodeRole.AuthBridge,
                        "IssueAuthTicket",
                        validation);
                    return Task.FromResult(
                        new WireV1.IssueAuthTicketResponse
                        {
                            Result = ToWireResult(validation)
                        });
                }

                AuthenticationTransportResultCode result =
                    _state.TryIssueTicketIdempotent(
                        request.AccountName,
                        request.AuthorizationCode,
                        Guid.ParseExact(request.InstallationId, "D"),
                        request.CountryId,
                        TimeSpan.FromSeconds(_options.TicketTtlSeconds));
                WriteAudit(
                    request.Context,
                    WireV1.ClusterNodeRole.AuthBridge,
                    "IssueAuthTicket",
                    result);
                return Task.FromResult(
                    new WireV1.IssueAuthTicketResponse
                    {
                        Result = ToWireResult(result)
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.ConsumeAuthTicketResponse> ConsumeAuthTicket(
        WireV1.ConsumeAuthTicketRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AuthenticationTransportResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        GameforgeAuthenticationContractValidator.Validate(
                            request),
                        WireV1.ClusterNodeRole.Login,
                        "ConsumeAuthTicket",
                        context);
                if (validation != AuthenticationTransportResultCode.Success)
                {
                    WriteAudit(
                        request?.Context,
                        WireV1.ClusterNodeRole.Login,
                        "ConsumeAuthTicket",
                        validation);
                    return Task.FromResult(
                        new WireV1.ConsumeAuthTicketResponse
                        {
                            Result = ToWireResult(validation)
                        });
                }

                AuthenticationTicketConsumptionResult result =
                    _state.TryConsumeTicket(
                        request.AuthorizationCode,
                        Guid.ParseExact(request.InstallationId, "D"),
                        request.CountryId,
                        request.ProposedSessionId);
                WriteAudit(
                    request.Context,
                    WireV1.ClusterNodeRole.Login,
                    "ConsumeAuthTicket",
                    result.Result);
                return Task.FromResult(
                    new WireV1.ConsumeAuthTicketResponse
                    {
                        Result = ToWireResult(result.Result),
                        AccountName = result.AccountName ?? string.Empty,
                        ConsumptionNumber = result.ConsumptionNumber,
                        SessionId = result.SessionId
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.IssueWorldPermitResponse> IssueWorldPermit(
        WireV1.IssueWorldPermitRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AuthenticationTransportResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        GameforgeAuthenticationContractValidator.Validate(
                            request),
                        WireV1.ClusterNodeRole.Login,
                        "IssueWorldPermit",
                        context);
                AuthenticationTransportResultCode result =
                    validation == AuthenticationTransportResultCode.Success
                        ? _state.TryIssuePermitIdempotent(
                            request.AccountId,
                            request.SessionId,
                            request.IpAddress,
                            TimeSpan.FromSeconds(
                                _options.PermitTtlSeconds))
                        : validation;
                WriteAudit(
                    request?.Context,
                    WireV1.ClusterNodeRole.Login,
                    "IssueWorldPermit",
                    result);
                return Task.FromResult(
                    new WireV1.IssueWorldPermitResponse
                    {
                        Result = ToWireResult(result)
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.ConsumeWorldPermitResponse> ConsumeWorldPermit(
        WireV1.ConsumeWorldPermitRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AuthenticationTransportResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        GameforgeAuthenticationContractValidator.Validate(
                            request),
                        WireV1.ClusterNodeRole.World,
                        "ConsumeWorldPermit",
                        context);
                AuthenticationTransportResultCode result =
                    validation == AuthenticationTransportResultCode.Success
                        ? _state.TryConsumePermit(
                            request.AccountId,
                            request.SessionId,
                            request.IpAddress)
                        : validation;
                WriteAudit(
                    request?.Context,
                    WireV1.ClusterNodeRole.World,
                    "ConsumeWorldPermit",
                    result);
                return Task.FromResult(
                    new WireV1.ConsumeWorldPermitResponse
                    {
                        Result = ToWireResult(result)
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.RevokeWorldPermitResponse> RevokeWorldPermit(
        WireV1.RevokeWorldPermitRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AuthenticationTransportResultCode validation =
                    ValidateAndAuthorize(
                        request?.Context,
                        GameforgeAuthenticationContractValidator.Validate(
                            request),
                        WireV1.ClusterNodeRole.Login,
                        "RevokeWorldPermit",
                        context);
                AuthenticationTransportResultCode result =
                    validation == AuthenticationTransportResultCode.Success
                        ? _state.RevokePermit(
                            request.AccountId,
                            request.SessionId)
                        : validation;
                WriteAudit(
                    request?.Context,
                    WireV1.ClusterNodeRole.Login,
                    "RevokeWorldPermit",
                    result);
                return Task.FromResult(
                    new WireV1.RevokeWorldPermitResponse
                    {
                        Result = ToWireResult(result)
                    });
            },
            context.CancellationToken);
    }

    private AuthenticationTransportResultCode ValidateAndAuthorize(
        WireV1.RequestContext requestContext,
        AuthenticationContractValidationError validationError,
        WireV1.ClusterNodeRole expectedRole,
        string operation,
        ServerCallContext callContext)
    {
        var certificate =
            callContext.GetHttpContext().Connection.ClientCertificate;
        if (!_roleMap.TryResolveRole(
                certificate,
                out WireV1.ClusterNodeRole certificateRole) ||
            certificateRole != expectedRole)
        {
            _logger.LogWarning(
                "Authentication RPC {Operation} rejected an unauthorized client certificate for expected role {ExpectedRole}.",
                operation,
                expectedRole);
            throw new RpcException(
                new Status(
                    StatusCode.PermissionDenied,
                    "The client certificate is not authorized for this operation."));
        }

        if (validationError != AuthenticationContractValidationError.None ||
            requestContext == null ||
            requestContext.CallerRole != certificateRole)
        {
            return AuthenticationTransportResultCode.InvalidRequest;
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
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        DateTime grpcDeadline = callContext.Deadline;
        if (grpcDeadline == DateTime.MaxValue)
        {
            return AuthenticationTransportResultCode.InvalidRequest;
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
            return AuthenticationTransportResultCode.InvalidRequest;
        }

        return _replayGuard.TryAccept(
            requestContext.RequestId,
            requestDeadline,
            now)
            ? AuthenticationTransportResultCode.Success
            : AuthenticationTransportResultCode.Conflict;
    }

    private void WriteAudit(
        WireV1.RequestContext requestContext,
        WireV1.ClusterNodeRole callerRole,
        string operation,
        AuthenticationTransportResultCode result)
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
            "Authentication RPC {Operation} completed with {Result}; caller role {CallerRole}; request {RequestId}.",
            operation,
            result,
            callerRole,
            requestId);
    }

    private static WireV1.AuthenticationResultCode ToWireResult(
        AuthenticationTransportResultCode result)
    {
        return (WireV1.AuthenticationResultCode)(int)result;
    }
}
