using NosGm.Configuration;
using NosGm.Core;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Configuration;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class ConfigurationServiceClient : IConfigurationService
    {
        #region Instantiation

        public ConfigurationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _confClient = new ConfigurationClient();
            _client = ScsServiceClientBuilder.CreateClient<IConfigurationService>(new ScsTcpEndPoint(ip, port),
                _confClient);
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: nameof(CommunicationServiceClient));
                    Thread.Sleep(1000);
                }

            string shadowDiagnostic;
            ConfigurationGrpcShadowMirror shadowMirror;
            if (ConfigurationGrpcShadowMirror.TryCreateFromEnvironment(
                    out shadowMirror,
                    out shadowDiagnostic))
            {
                _grpcShadowMirror = shadowMirror;
                Logger.Info(
                    "[CONFIG_GRPC_SHADOW] Configuration shadow mirror enabled; SCS remains authoritative.");
            }
            else if (!string.Equals(
                         shadowDiagnostic,
                         "disabled",
                         StringComparison.Ordinal))
            {
                Logger.Warn(
                    "[CONFIG_GRPC_SHADOW] Shadow mirror unavailable; continuing with SCS authority. Reason=" +
                    (shadowDiagnostic ?? "unknown"));
            }
        }

        #endregion

        #region Events

        public event EventHandler ConfigurationUpdate;

        #endregion

        #region Members

        private static ConfigurationServiceClient _instance;

        private readonly IScsServiceClient<IConfigurationService> _client;

        private readonly ConfigurationClient _confClient;

        private readonly ConfigurationGrpcShadowMirror _grpcShadowMirror;

        #endregion

        #region Properties

        public static ConfigurationServiceClient Instance =>
            _instance ?? (_instance = new ConfigurationServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            return _client.ServiceProxy.Authenticate(authKey, serverId);
        }

        public ConfigurationObject GetConfigurationObject()
        {
            ConfigurationObject authoritative =
                _client.ServiceProxy.GetConfigurationObject();
            ObserveAuthoritativeConfiguration(authoritative, "Get");
            return authoritative;
        }

        public void UpdateConfigurationObject(ConfigurationObject configurationObject)
        {
            _client.ServiceProxy.UpdateConfigurationObject(configurationObject);
            ObserveAuthoritativeConfiguration(configurationObject, "Update");
        }

        internal void OnConfigurationUpdated(ConfigurationObject configurationObject)
        {
            ConfigurationUpdate?.Invoke(configurationObject, null);
        }

        private void ObserveAuthoritativeConfiguration(
            ConfigurationObject authoritative,
            string source)
        {
            if (_grpcShadowMirror == null)
            {
                return;
            }

            ConfigurationGrpcShadowResult result =
                _grpcShadowMirror.Synchronize(authoritative);
            switch (result.Status)
            {
                case ConfigurationGrpcShadowStatus.Matched:
                    return;
                case ConfigurationGrpcShadowStatus.Seeded:
                case ConfigurationGrpcShadowStatus.Resynchronized:
                    Logger.Info(
                        "[CONFIG_GRPC_SHADOW] " + source +
                        " synchronized shadow generation " +
                        result.Generation + ". SCS remains authoritative.");
                    return;
                default:
                    Logger.Warn(
                        "[CONFIG_GRPC_SHADOW] " + source +
                        " shadow observation failed with " +
                        result.Status + "/" + result.TransportResult +
                        "; SCS result remains authoritative.");
                    return;
            }
        }

        #endregion
    }
}