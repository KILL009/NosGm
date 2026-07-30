using Grpc.Core;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.Services;

public sealed class ClusterCommunicationService
    : WireV1.ClusterCommunication.ClusterCommunicationBase
{
    private readonly CommunicationCallbackHub _callbackHub;
    private readonly AuthenticationDispatchGate _dispatchGate;
    private readonly ILogger<ClusterCommunicationService> _logger;
    private readonly AuthenticationRequestReplayGuard _replayGuard;
    private readonly ClientCertificateRoleMap _roleMap;
    private readonly ClusterCommunicationState _state;
    private readonly TimeProvider _timeProvider;

    public ClusterCommunicationService(
        AuthenticationDispatchGate dispatchGate,
        AuthenticationRequestReplayGuard replayGuard,
        ClientCertificateRoleMap roleMap,
        ClusterCommunicationState state,
        CommunicationCallbackHub callbackHub,
        TimeProvider timeProvider,
        ILogger<ClusterCommunicationService> logger)
    {
        _dispatchGate = dispatchGate;
        _replayGuard = replayGuard;
        _roleMap = roleMap;
        _state = state;
        _callbackHub = callbackHub;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override Task<WireV1.CommunicationMutationResponse>
        RegisterAccountLogin(
            WireV1.RegisterAccountLoginRequest request,
            ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator.Validate(request),
            "RegisterAccountLogin",
            context,
            new[] { WireV1.ClusterNodeRole.Login },
            () => _state.RegisterAccountLogin(
                request.AccountId,
                request.SessionId,
                request.IpAddress));
    }

    public override Task<WireV1.CommunicationBooleanResponse>
        IsAccountSessionRegistered(
            WireV1.AccountSessionRequest request,
            ServerCallContext context)
    {
        return RunBooleanAsync(
            request?.Context,
            ClusterCommunicationContractValidator
                .ValidateAccountSessionRegistered(request),
            "IsAccountSessionRegistered",
            context,
            new[] { WireV1.ClusterNodeRole.Login },
            () => _state.IsAccountSessionRegistered(
                request.AccountId,
                request.SessionId));
    }

    public override Task<WireV1.CommunicationBooleanResponse> IsLoginPermitted(
        WireV1.AccountSessionRequest request,
        ServerCallContext context)
    {
        return RunBooleanAsync(
            request?.Context,
            ClusterCommunicationContractValidator.ValidateLoginPermitted(request),
            "IsLoginPermitted",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () => _state.IsLoginPermitted(
                request.AccountId,
                request.SessionId));
    }

    public override Task<WireV1.CommunicationBooleanResponse> IsAccountConnected(
        WireV1.AccountRequest request,
        ServerCallContext context)
    {
        return RunBooleanAsync(
            request?.Context,
            ClusterCommunicationContractValidator
                .ValidateAccountConnected(request),
            "IsAccountConnected",
            context,
            new[]
            {
                WireV1.ClusterNodeRole.Login,
                WireV1.ClusterNodeRole.World
            },
            () => _state.IsAccountConnected(request.AccountId));
    }

    public override Task<WireV1.CommunicationMutationResponse> ConnectAccount(
        WireV1.ConnectAccountRequest request,
        ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator.Validate(request),
            "ConnectAccount",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () => _state.ConnectAccount(
                Guid.ParseExact(request.WorldId, "D"),
                request.AccountId,
                request.SessionId));
    }

    public override Task<WireV1.CommunicationMutationResponse> DisconnectAccount(
        WireV1.DisconnectAccountRequest request,
        ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator.Validate(request),
            "DisconnectAccount",
            context,
            new[]
            {
                WireV1.ClusterNodeRole.Login,
                WireV1.ClusterNodeRole.World
            },
            () =>
            {
                WireV1.CommunicationResultCode result =
                    _state.DisconnectAccount(
                        request.AccountId,
                        request.SessionId,
                        request.PreserveSessionRegistration);
                if (result == WireV1.CommunicationResultCode.Success)
                {
                    _callbackHub.DisconnectAccount(
                        request.AccountId,
                        request.SessionId);
                }
                return result;
            });
    }

    public override Task<WireV1.CommunicationMutationResponse> PulseAccount(
        WireV1.AccountSessionRequest request,
        ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator.ValidatePulse(request),
            "PulseAccount",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () =>
            {
                WireV1.CommunicationResultCode result = _state.PulseAccount(
                    request.AccountId,
                    request.SessionId);
                if (result == WireV1.CommunicationResultCode.Success)
                {
                    _callbackHub.PulseAccount(
                        request.AccountId,
                        request.SessionId);
                }
                return result;
            });
    }

    public override Task<WireV1.CommunicationMutationResponse> ConnectCharacter(
        WireV1.CharacterWorldRequest request,
        ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator
                .ValidateConnectCharacter(request),
            "ConnectCharacter",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () =>
            {
                Guid worldId = Guid.ParseExact(request.WorldId, "D");
                WireV1.CommunicationResultCode result =
                    _state.ConnectCharacter(
                        worldId,
                        request.AccountId,
                        request.SessionId,
                        request.CharacterId);
                if (result == WireV1.CommunicationResultCode.Success)
                {
                    _callbackHub.BindCharacter(
                        worldId,
                        request.AccountId,
                        request.SessionId,
                        request.CharacterId);
                }
                return result;
            });
    }

    public override Task<WireV1.CommunicationMutationResponse>
        DisconnectCharacter(
            WireV1.CharacterWorldRequest request,
            ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator
                .ValidateDisconnectCharacter(request),
            "DisconnectCharacter",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () =>
            {
                Guid worldId = Guid.ParseExact(request.WorldId, "D");
                WireV1.CommunicationResultCode result =
                    _state.DisconnectCharacter(
                        worldId,
                        request.AccountId,
                        request.SessionId,
                        request.CharacterId);
                if (result == WireV1.CommunicationResultCode.Success)
                {
                    _callbackHub.UnbindCharacter(
                        worldId,
                        request.AccountId,
                        request.SessionId,
                        request.CharacterId);
                }
                return result;
            });
    }

    public override Task<WireV1.RegisterWorldServerResponse> RegisterWorldServer(
        WireV1.RegisterWorldServerRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.CommunicationResultCode validation = ValidateAndAuthorize(
                    request?.Context,
                    ClusterCommunicationContractValidator.Validate(request),
                    "RegisterWorldServer",
                    context,
                    WireV1.ClusterNodeRole.World);
                Guid worldId = validation == WireV1.CommunicationResultCode.Success
                    ? Guid.ParseExact(request.World.WorldId, "D")
                    : Guid.Empty;
                ClusterCommunicationState.RegisterWorldResult result =
                    validation == WireV1.CommunicationResultCode.Success
                        ? _state.RegisterWorldServer(
                            worldId,
                            request.World.EndpointIp,
                            checked((int)request.World.EndpointPort),
                            checked((int)request.World.AccountLimit),
                            request.World.WorldGroup)
                        : new ClusterCommunicationState.RegisterWorldResult
                        {
                            Result = validation
                        };
                if (result.Result == WireV1.CommunicationResultCode.Success)
                {
                    WireV1.CommunicationResultCode routeResult =
                        _callbackHub.RegisterWorld(
                            worldId,
                            result.ChannelId,
                            request.World.WorldGroup);
                    if (routeResult != WireV1.CommunicationResultCode.Success)
                    {
                        _state.UnregisterWorldServer(worldId);
                        result = new ClusterCommunicationState.RegisterWorldResult
                        {
                            Result = routeResult
                        };
                    }
                }
                WriteAudit(
                    request?.Context,
                    "RegisterWorldServer",
                    result.Result);
                return Task.FromResult(
                    new WireV1.RegisterWorldServerResponse
                    {
                        Result = result.Result,
                        ChannelId = result.ChannelId
                    });
            },
            context.CancellationToken);
    }

    public override Task<WireV1.CommunicationMutationResponse>
        UnregisterWorldServer(
            WireV1.WorldRequest request,
            ServerCallContext context)
    {
        return RunMutationAsync(
            request?.Context,
            ClusterCommunicationContractValidator.Validate(request),
            "UnregisterWorldServer",
            context,
            new[] { WireV1.ClusterNodeRole.World },
            () =>
            {
                Guid worldId = Guid.ParseExact(request.WorldId, "D");
                WireV1.CommunicationResultCode result =
                    _state.UnregisterWorldServer(worldId);
                if (result == WireV1.CommunicationResultCode.Success)
                {
                    _callbackHub.UnregisterWorld(worldId);
                }
                return result;
            });
    }

    public override Task<WireV1.ListWorldServersResponse> ListWorldServers(
        WireV1.ListWorldServersRequest request,
        ServerCallContext context)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.CommunicationResultCode validation = ValidateAndAuthorize(
                    request?.Context,
                    ClusterCommunicationContractValidator.Validate(request),
                    "ListWorldServers",
                    context,
                    WireV1.ClusterNodeRole.Login);
                var response = new WireV1.ListWorldServersResponse
                {
                    Result = validation
                };
                if (validation == WireV1.CommunicationResultCode.Success)
                {
                    foreach (ClusterCommunicationState.WorldSnapshot world in
                             _state.ListVisibleWorldServers())
                    {
                        response.Worlds.Add(new WireV1.WorldChannelSnapshot
                        {
                            WorldId = world.WorldId.ToString("D"),
                            EndpointIp = world.EndpointIp,
                            EndpointPort = checked((uint)world.EndpointPort),
                            AccountLimit = checked((uint)world.AccountLimit),
                            ConnectedAccounts =
                                checked((uint)world.ConnectedAccounts),
                            ChannelId = world.ChannelId,
                            WorldGroup = world.WorldGroup
                        });
                    }
                }

                WriteAudit(
                    request?.Context,
                    "ListWorldServers",
                    response.Result);
                return Task.FromResult(response);
            },
            context.CancellationToken);
    }

    private Task<WireV1.CommunicationMutationResponse> RunMutationAsync(
        WireV1.RequestContext requestContext,
        CommunicationContractValidationError validationError,
        string operation,
        ServerCallContext context,
        IReadOnlyCollection<WireV1.ClusterNodeRole> allowedRoles,
        Func<WireV1.CommunicationResultCode> action)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.CommunicationResultCode validation = ValidateAndAuthorize(
                    requestContext,
                    validationError,
                    operation,
                    context,
                    allowedRoles.ToArray());
                WireV1.CommunicationResultCode result =
                    validation == WireV1.CommunicationResultCode.Success
                        ? action()
                        : validation;
                WriteAudit(requestContext, operation, result);
                return Task.FromResult(
                    new WireV1.CommunicationMutationResponse
                    {
                        Result = result
                    });
            },
            context.CancellationToken);
    }

    private Task<WireV1.CommunicationBooleanResponse> RunBooleanAsync(
        WireV1.RequestContext requestContext,
        CommunicationContractValidationError validationError,
        string operation,
        ServerCallContext context,
        IReadOnlyCollection<WireV1.ClusterNodeRole> allowedRoles,
        Func<bool> action)
    {
        return _dispatchGate.RunAsync(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WireV1.CommunicationResultCode validation = ValidateAndAuthorize(
                    requestContext,
                    validationError,
                    operation,
                    context,
                    allowedRoles.ToArray());
                bool value = false;
                if (validation == WireV1.CommunicationResultCode.Success)
                {
                    value = action();
                }

                WriteAudit(requestContext, operation, validation);
                return Task.FromResult(
                    new WireV1.CommunicationBooleanResponse
                    {
                        Result = validation,
                        Value = value
                    });
            },
            context.CancellationToken);
    }

    private WireV1.CommunicationResultCode ValidateAndAuthorize(
        WireV1.RequestContext requestContext,
        CommunicationContractValidationError validationError,
        string operation,
        ServerCallContext callContext,
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
                "Communication RPC {Operation} rejected an unauthorized client certificate.",
                operation);
            throw new RpcException(
                new Status(
                    StatusCode.PermissionDenied,
                    "The client certificate is not authorized for this operation."));
        }

        if (validationError != CommunicationContractValidationError.None ||
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

        return _replayGuard.TryAccept(
            requestContext.RequestId,
            requestDeadline,
            now)
            ? WireV1.CommunicationResultCode.Success
            : WireV1.CommunicationResultCode.Conflict;
    }

    private void WriteAudit(
        WireV1.RequestContext requestContext,
        string operation,
        WireV1.CommunicationResultCode result)
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
            "Communication RPC {Operation} completed with {Result}; request {RequestId}.",
            operation,
            result,
            requestId);
    }
}
