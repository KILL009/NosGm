using NosGm.Core.Diagnostics;
using NosGm.Core.Networking.Communication.Scs.Communication;
using NosGm.Core.Networking.Communication.Scs.Communication.Channels;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using NosGm.Core.Networking.Communication.Scs.Server;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.Core
{
    public class NetworkClient : ScsServerClient, INetworkClient
    {
        #region Instantiation

        public NetworkClient(ICommunicationChannel communicationChannel) : base(communicationChannel)
        {
            MessageReceived += RecordReceivedMessage;
            MessageSent += RecordSentMessage;
        }

        #endregion

        #region Members

        private CryptographyBase _encryptor;
        private object _session;

        #endregion

        #region Properties

        public string IpAddress => RemoteEndPoint.ToString();

        public bool IsConnected => CommunicationState == CommunicationStates.Connected;

        public bool IsDisposing { get; set; }

        #endregion

        #region Methods

        public void Initialize(CryptographyBase encryptor)
        {
            _encryptor = encryptor;
        }

        public void SendPacket(string packet, byte priority = 10)
        {
            if (!IsDisposing && packet != null && packet != "")
            {
                var rawMessage = new ScsRawDataMessage(_encryptor.Encrypt(packet));
                SendMessage(rawMessage, priority);
            }
        }

        public async Task SendPacketAsync(string packet, byte priority = 10)
        {
            ScsRawDataMessage rawDataMessage = new ScsRawDataMessage(_encryptor.Encrypt(packet));
            await SendMessageAsync(rawDataMessage, priority).ConfigureAwait(false);
        }

        public void SendPacketFormat(string packet, params object[] param)
        {
            SendPacket(string.Format(packet, param));
        }

        public void SendPackets(IEnumerable<string> packets, byte priority = 10)
        {
            foreach (var packet in packets) SendPacket(packet, priority);
        }

        public async Task SendPacketsAsync(IEnumerable<string> packets, byte priority = 10)
        {
            foreach (string packet in packets)
            {
                await SendPacketAsync(packet, priority);
            }
        }

        public void SetClientSession(object clientSession)
        {
            _session = clientSession;
        }

        private static void RecordReceivedMessage(object sender, MessageEventArgs eventArgs)
        {
            if (eventArgs?.Message is ScsRawDataMessage rawMessage)
            {
                ServerPerformanceMonitor.Instance.RecordReceived(rawMessage.MessageData?.Length ?? 0);
            }
        }

        private static void RecordSentMessage(object sender, MessageEventArgs eventArgs)
        {
            if (eventArgs?.Message is ScsRawDataMessage rawMessage)
            {
                ServerPerformanceMonitor.Instance.RecordSent(rawMessage.MessageData?.Length ?? 0);
            }
        }

        #endregion
    }
}
