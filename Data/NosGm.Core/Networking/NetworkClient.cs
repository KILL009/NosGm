using NosGm.Core.Diagnostics;
using NosGm.Core.Networking.Communication.Scs.Communication;
using NosGm.Core.Networking.Communication.Scs.Communication.Channels;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using NosGm.Core.Networking.Communication.Scs.Server;
using System;
using System.Collections.Generic;
using System.Threading;
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

        private const int MaximumInitialCustomParameterBytes = 4096;

        private CryptographyBase _encryptor;
        private object _session;
        private readonly object _initialCustomParameterSync = new object();
        private byte[] _pendingInitialCustomParameterBytes = Array.Empty<byte>();
        private int _initialCustomParameterFrameSplit;

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

        /// <summary>
        /// The current Steam/Gameforge client may coalesce the initial World custom
        /// parameter and the first encrypted packets in one transport message. Split
        /// at the protocol terminator and raise both logical messages in order so the
        /// session parser cannot discard the encrypted tail.
        /// </summary>
        protected override IEnumerable<IScsMessage> TransformReceivedMessages(IScsMessage message)
        {
            if (!(message is ScsRawDataMessage rawMessage) ||
                _encryptor?.HasCustomParameter != true ||
                Volatile.Read(ref _initialCustomParameterFrameSplit) != 0 ||
                rawMessage.MessageData == null)
            {
                yield return message;
                yield break;
            }

            byte[] customParameterFrame = null;
            byte[] remainder = null;
            bool waitingForTerminator = false;
            bool frameTooLarge = false;

            lock (_initialCustomParameterSync)
            {
                int pendingLength = _pendingInitialCustomParameterBytes.Length;
                int incomingLength = rawMessage.MessageData.Length;
                if (pendingLength + incomingLength > MaximumInitialCustomParameterBytes)
                {
                    _pendingInitialCustomParameterBytes = Array.Empty<byte>();
                    frameTooLarge = true;
                }
                else
                {
                    var candidate = new byte[pendingLength + incomingLength];
                    if (pendingLength > 0)
                    {
                        Buffer.BlockCopy(_pendingInitialCustomParameterBytes, 0, candidate, 0, pendingLength);
                    }
                    Buffer.BlockCopy(rawMessage.MessageData, 0, candidate, pendingLength, incomingLength);

                    if (!TrySplitInitialCustomParameterFrame(candidate, out customParameterFrame, out remainder))
                    {
                        _pendingInitialCustomParameterBytes = candidate;
                        waitingForTerminator = true;
                    }
                    else
                    {
                        _pendingInitialCustomParameterBytes = Array.Empty<byte>();
                        Interlocked.Exchange(ref _initialCustomParameterFrameSplit, 1);
                    }
                }
            }

            if (frameTooLarge)
            {
                Logger.Warn(
                    $"Initial custom-parameter frame exceeded {MaximumInitialCustomParameterBytes} bytes for client {ClientId} ({RemoteEndPoint}).");
                Disconnect();
                yield break;
            }

            if (waitingForTerminator)
            {
                yield break;
            }

            yield return new ScsRawDataMessage(customParameterFrame);
            if (remainder.Length > 0)
            {
                yield return new ScsRawDataMessage(remainder);
            }
        }

        private static bool TrySplitInitialCustomParameterFrame(
            byte[] data,
            out byte[] customParameterFrame,
            out byte[] remainder)
        {
            customParameterFrame = null;
            remainder = null;
            if (data == null || data.Length < 2)
            {
                return false;
            }

            int terminatorIndex = Array.IndexOf(data, (byte)0x0E, 1);
            if (terminatorIndex < 1)
            {
                return false;
            }

            int customLength = terminatorIndex + 1;
            customParameterFrame = new byte[customLength];
            Buffer.BlockCopy(data, 0, customParameterFrame, 0, customLength);

            int remainingLength = data.Length - customLength;
            remainder = new byte[remainingLength];
            if (remainingLength > 0)
            {
                Buffer.BlockCopy(data, customLength, remainder, 0, remainingLength);
            }
            return true;
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
