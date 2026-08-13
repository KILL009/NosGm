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

        private const ulong FnvOffsetBasis = 14695981039346656037UL;

        private const ulong FnvPrime = 1099511628211UL;

        private static readonly HashSet<string> ModernFourArgumentWopenTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "8",
                "9",
                "27",
                "93"
            };

        private CryptographyBase _encryptor;
        private object _session;
        private readonly object _initialCustomParameterSync = new object();
        private byte[] _pendingInitialCustomParameterBytes = Array.Empty<byte>();
        private int _initialCustomParameterFrameSplit;
        private int _initialCustomParameterFragments;

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
                packet = NormalizeOfficialPacketLayout(packet);
                priority = NormalizePacketPriority(packet, priority);
                ScsRawDataMessage rawMessage = CreateRawMessage(packet);
                SendMessage(rawMessage, priority);
            }
        }

        public async Task SendPacketAsync(string packet, byte priority = 10)
        {
            packet = NormalizeOfficialPacketLayout(packet);
            priority = NormalizePacketPriority(packet, priority);
            ScsRawDataMessage rawDataMessage = CreateRawMessage(packet);
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
        /// Keeps high-priority capacity available for critical World state while
        /// movement fan-out is under pressure. Movement is transient visual state,
        /// so packets using the normal priority are routed through the low-priority
        /// transport path. Explicit non-default priorities remain untouched.
        /// </summary>
        private static byte NormalizePacketPriority(string packet, byte priority)
        {
            return priority == 10 &&
                   packet != null &&
                   packet.StartsWith("mv ", StringComparison.Ordinal)
                ? (byte)5
                : priority;
        }

        /// <summary>
        /// Creates the encrypted wire message and tags movement as replaceable state.
        /// The transient key hashes only the stable "mv type callerId" prefix, so a
        /// newer position for the same entity can replace an older unsent position
        /// without allocating Split/Substring objects on the hot broadcast path.
        /// </summary>
        private ScsRawDataMessage CreateRawMessage(string packet)
        {
            var rawMessage = new ScsRawDataMessage(_encryptor.Encrypt(packet));
            if (TryGetMovementTransientKey(packet, out long transientKey))
            {
                rawMessage.IsTransient = true;
                rawMessage.TransientKey = transientKey;
            }

            return rawMessage;
        }

        private static bool TryGetMovementTransientKey(string packet, out long transientKey)
        {
            transientKey = 0;
            if (string.IsNullOrEmpty(packet) ||
                !packet.StartsWith("mv ", StringComparison.Ordinal))
            {
                return false;
            }

            unchecked
            {
                ulong hash = FnvOffsetBasis;
                int spaces = 0;

                for (int index = 0; index < packet.Length; index++)
                {
                    char value = packet[index];
                    hash ^= value;
                    hash *= FnvPrime;

                    if (value != ' ')
                    {
                        continue;
                    }

                    spaces++;
                    if (spaces != 3)
                    {
                        continue;
                    }

                    transientKey = (long)(hash == 0 ? 1UL : hash);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Normalizes the current official layout for the window packets observed in
        /// the live client captures. Older handlers still emit two or three payload
        /// values for these window types, while the current client emits four.
        /// Unknown and payload-bearing window types are deliberately left untouched.
        /// </summary>
        private static string NormalizeOfficialPacketLayout(string packet)
        {
            if (string.IsNullOrWhiteSpace(packet) ||
                !packet.StartsWith("wopen ", StringComparison.Ordinal))
            {
                return packet;
            }

            string[] fields = packet.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length < 2 ||
                fields.Length >= 5 ||
                !ModernFourArgumentWopenTypes.Contains(fields[1]))
            {
                return packet;
            }

            switch (fields.Length)
            {
                case 2:
                    return $"wopen {fields[1]} 0 0 0";
                case 3:
                    return $"wopen {fields[1]} {fields[2]} 0 0";
                case 4:
                    return $"wopen {fields[1]} {fields[2]} {fields[3]} 0";
                default:
                    return packet;
            }
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
                _initialCustomParameterFragments++;
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
                    $"[WORLD_HANDSHAKE] Stage=REJECTED Code=INITIAL_FRAME_TOO_LARGE ClientId={ClientId} " +
                    $"LimitBytes={MaximumInitialCustomParameterBytes} Fragments={_initialCustomParameterFragments}");
                Disconnect();
                yield break;
            }

            if (waitingForTerminator)
            {
                Logger.Info(
                    $"[WORLD_HANDSHAKE] Stage=INITIAL_FRAME_BUFFERED ClientId={ClientId} " +
                    $"PendingBytes={_pendingInitialCustomParameterBytes.Length} Fragments={_initialCustomParameterFragments}");
                yield break;
            }

            Logger.Info(
                $"[WORLD_HANDSHAKE] Stage=INITIAL_FRAME_SPLIT ClientId={ClientId} " +
                $"CustomBytes={customParameterFrame.Length} TailBytes={remainder.Length} " +
                $"Fragments={_initialCustomParameterFragments}");
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
