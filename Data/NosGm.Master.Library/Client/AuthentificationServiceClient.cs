using NosGm.Authentication.Client;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.V1;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    public class AuthentificationServiceClient : IAuthentificationService
    {
        private static AuthentificationServiceClient _instance;
        private readonly object _transportSync = new object();
        private readonly Dictionary<ClusterNodeRole, IGameforgeAuthenticationTransport>
            _transports =
                new Dictionary<ClusterNodeRole, IGameforgeAuthenticationTransport>();
        private readonly AuthenticationTransportMode _transportMode;
        private readonly IScsServiceClient<IAuthentificationService> _client;

        public AuthentificationServiceClient()
        {
            _transportMode =
                AuthenticationTransportModeParser.ParseEnvironment();
            if (_transportMode == AuthenticationTransportMode.Grpc)
            {
                return;
            }

            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _client = ScsServiceClientBuilder.CreateClient<IAuthentificationService>(new ScsTcpEndPoint(ip, port));
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
            {
                try { _client.Connect(); }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"), memberName: nameof(AuthentificationServiceClient));
                    Thread.Sleep(1000);
                }
            }
        }

        public static AuthentificationServiceClient Instance => _instance ?? (_instance = new AuthentificationServiceClient());
        public CommunicationStates CommunicationState =>
            _transportMode == AuthenticationTransportMode.Grpc
                ? CommunicationStates.Connected
                : _client.CommunicationState;

        public bool Authenticate(string authKey)
        {
            if (_transportMode == AuthenticationTransportMode.Scs)
            {
                return _client.ServiceProxy.Authenticate(authKey);
            }

            if (string.Equals(
                    authKey,
                    ServerConfiguration.GameforgeTicketIssuerKey,
                    StringComparison.Ordinal))
            {
                GetTransport(ClusterNodeRole.AuthBridge);
                return true;
            }

            if (string.Equals(
                    authKey,
                    ServerConfiguration.GameforgeTicketConsumerKey,
                    StringComparison.Ordinal))
            {
                GetTransport(ClusterNodeRole.Login);
                return true;
            }

            if (string.Equals(
                    authKey,
                    ServerConfiguration.AuthServiceKey,
                    StringComparison.Ordinal))
            {
                GetTransport(ClusterNodeRole.World);
                return true;
            }

            return false;
        }

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            RequireScsForLegacyValidation();
            return _client.ServiceProxy.ValidateAccount(userName, passHash);
        }

        public CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash)
        {
            RequireScsForLegacyValidation();
            return _client.ServiceProxy.ValidateAccountAndCharacter(
                userName,
                characterName,
                passHash);
        }

        public bool RegisterGameforgeAuthTicket(string accountName, string authToken, string installationId, byte countryId)
        {
            return GetTransport(ClusterNodeRole.AuthBridge)
                       .IssueAuthTicketAsync(
                           accountName,
                           authToken,
                           installationId,
                           countryId,
                           CancellationToken.None)
                       .GetAwaiter()
                       .GetResult() ==
                   AuthenticationTransportResultCode.Success;
        }

        public GameforgeAuthTicketConsumption ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId,
            int proposedSessionId)
        {
            AuthenticationTicketConsumptionResult result =
                GetTransport(ClusterNodeRole.Login)
                    .ConsumeAuthTicketAsync(
                        authToken,
                        installationId,
                        countryId,
                        proposedSessionId,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            return result.IsSuccess
                ? new GameforgeAuthTicketConsumption
                {
                    AccountName = result.AccountName,
                    ConsumptionNumber = result.ConsumptionNumber,
                    SessionId = result.SessionId
                }
                : null;
        }

        public bool RegisterGameforgeWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            return GetTransport(ClusterNodeRole.Login)
                       .IssueWorldPermitAsync(
                           accountId,
                           sessionId,
                           ipAddress,
                           CancellationToken.None)
                       .GetAwaiter()
                       .GetResult() ==
                   AuthenticationTransportResultCode.Success;
        }

        public bool ConsumeGameforgeWorldPermit(long accountId, int sessionId, string ipAddress)
        {
            return ConsumeGameforgeWorldPermit(
                    accountId,
                    sessionId,
                    ipAddress,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<bool> ConsumeGameforgeWorldPermit(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            AuthenticationTransportResultCode result =
                await GetTransport(ClusterNodeRole.World)
                    .ConsumeWorldPermitAsync(
                        accountId,
                        sessionId,
                        ipAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            return result == AuthenticationTransportResultCode.Success;
        }

        public void RevokeGameforgeWorldPermit(long accountId, int sessionId)
        {
            GetTransport(ClusterNodeRole.Login)
                .RevokeWorldPermitAsync(
                    accountId,
                    sessionId,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        private IGameforgeAuthenticationTransport GetTransport(
            ClusterNodeRole role)
        {
            lock (_transportSync)
            {
                if (_transports.TryGetValue(
                        role,
                        out IGameforgeAuthenticationTransport existing))
                {
                    return existing;
                }

                IGameforgeAuthenticationTransport scsTransport = null;
                IGameforgeAuthenticationTransport grpcTransport = null;
                if (_transportMode == AuthenticationTransportMode.Scs)
                {
                    scsTransport =
                        new ScsGameforgeAuthenticationTransport(
                            _client.ServiceProxy);
                }
                else
                {
                    grpcTransport =
                        new GrpcGameforgeAuthenticationTransport(
                            AuthenticationGrpcClientOptions.Load(role));
                }

                var selected = new AuthenticationTransportRouter(
                    _transportMode,
                    scsTransport,
                    grpcTransport);
                _transports.Add(role, selected);
                return selected;
            }
        }

        private void RequireScsForLegacyValidation()
        {
            if (_transportMode != AuthenticationTransportMode.Scs)
            {
                throw new InvalidOperationException(
                    "Legacy account validation is unavailable when the authentication transport is GRPC.");
            }
        }
    }
}
