using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints;
using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Core.Networking.Communication.Scs.Communication.Channels.Tcp
{
    /// <summary>
    ///     This class is used to communicate with a remote application over TCP/IP protocol.
    /// </summary>
    public class TcpCommunicationChannel : CommunicationChannelBase, IDisposable
    {
        #region Instantiation

        /// <summary>
        ///     Creates a new TcpCommunicationChannel object.
        /// </summary>
        /// <param name="clientSocket">
        ///     A connected Socket object that is used to communicate over network
        /// </param>
        public TcpCommunicationChannel(Socket clientSocket)
        {
            _clientSocket = clientSocket;
            _clientSocket.NoDelay = true;
            var ipEndPoint = (IPEndPoint)_clientSocket.RemoteEndPoint;
            _remoteEndPoint = new ScsTcpEndPoint(ipEndPoint.Address, ipEndPoint.Port);
            _buffer = new byte[RECEIVE_BUFFER_SIZE];
            _highPriorityBuffer = new ConcurrentQueue<byte[]>();
            _lowPriorityBuffer = new ConcurrentQueue<byte[]>();
            _transientBuffer = new ConcurrentDictionary<long, byte[]>();
            _transientKeys = new ConcurrentQueue<long>();
            _sendSignal = new SemaphoreSlim(0, 1);
            _sendTask = RunSendPumpAsync(_sendCancellationToken.Token);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the endpoint of remote application.
        /// </summary>
        public override ScsEndPoint RemoteEndPoint => _remoteEndPoint;

        #endregion

        #region Members

        private const ushort PING_REQUEST = 0x0779;

        private const ushort PING_RESPONSE = 0x0988;

        /// <summary>
        ///     Size of the buffer that is used to receive bytes from TCP socket.
        /// </summary>
        private const int RECEIVE_BUFFER_SIZE = 4 * 1024; // 4KB

        private const int MaximumPacketsPerBatch = 256;

        private const int MaximumBatchBytes = 64 * 1024;

        /// <summary>
        /// Bounds the number of distinct replaceable visual states waiting on one
        /// connection. In practice this is the number of moving entities visible to
        /// the client, not the number of movement packets produced over time.
        /// </summary>
        private const int MaximumTransientStates = 4096;

        /// <summary>
        ///     This buffer is used to receive bytes
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        ///     Socket object to send/reveice messages.
        /// </summary>
        private readonly Socket _clientSocket;

        private readonly ConcurrentQueue<byte[]> _highPriorityBuffer;

        private readonly ConcurrentQueue<byte[]> _lowPriorityBuffer;

        /// <summary>
        /// Latest pending transient state keyed by source entity. A newer movement
        /// replaces an older unsent movement for the same entity instead of growing
        /// the socket backlog without bound.
        /// </summary>
        private readonly ConcurrentDictionary<long, byte[]> _transientBuffer;

        private readonly ConcurrentQueue<long> _transientKeys;

        private readonly ScsTcpEndPoint _remoteEndPoint;

        private readonly CancellationTokenSource _sendCancellationToken = new CancellationTokenSource();

        private readonly SemaphoreSlim _sendSignal;

        private readonly Task _sendTask;

        private bool _disposed;

        /// <summary>
        ///     A flag to control thread's running
        /// </summary>
        private volatile bool _running;

        #endregion

        #region Methods

        public override Task ClearLowPriorityQueueAsync()
        {
            _lowPriorityBuffer.Clear();
            _transientBuffer.Clear();
            while (_transientKeys.TryDequeue(out _))
            {
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Disconnects from remote application and closes channel.
        /// </summary>
        public override void Disconnect()
        {
            if (CommunicationState != CommunicationStates.Connected) return;

            _running = false;
            try
            {
                if (!_sendCancellationToken.IsCancellationRequested)
                {
                    _sendCancellationToken.Cancel();
                }

                TrySignalSendPump();

                if (_clientSocket.Connected)
                {
                    try
                    {
                        _clientSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException)
                    {
                        // The peer may already have closed the socket.
                    }
                }

                _clientSocket.Close();
                _clientSocket.Dispose();
            }
            catch (Exception)
            {
                // do nothing
            }

            CommunicationState = CommunicationStates.Disconnected;
            OnDisconnected();
        }

        /// <summary>
        ///     Calls Disconnect method.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        /// <summary>
        ///     Duplicates the client socket and closes.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <returns></returns>
        /// <summary>
        ///     The callee should dispose anything relying on this channel immediately.
        /// </summary>
        public SocketInformation DuplicateSocketAndClose(int processId)
        {
            // request ping from host to kill our async BeginReceive
            _clientSocket.Send(BitConverter.GetBytes(PING_REQUEST));

            // wait for response
            while (_running) Thread.Sleep(20);

            return _clientSocket.DuplicateAndClose(processId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }

        /// <summary>
        ///     Sends a message to the remote application.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        /// <param name="priority">Priority of message to send</param>
        protected override void SendMessagePublic(IScsMessage message, byte priority)
        {
            EnqueueMessage(message, priority);
        }

        /// <summary>
        ///     Starts the thread to receive messages from socket.
        /// </summary>
        protected override void StartPublic()
        {
            _running = true;
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, 0, ReceiveCallback, null);
        }

        /// <summary>
        ///     This method is used as callback method in _clientSocket's BeginReceive method. It
        ///     reveives bytes from socker.
        /// </summary>
        /// <param name="result">Asyncronous call result</param>
        private void ReceiveCallback(IAsyncResult result)
        {
            if (!_running) return;

            try
            {
                var bytesRead = _clientSocket.EndReceive(result);

                if (bytesRead > 0)
                {
                    if (bytesRead >= sizeof(ushort))
                    {
                        switch (BitConverter.ToUInt16(_buffer, 0))
                        {
                            case PING_REQUEST:
                                _clientSocket.Send(BitConverter.GetBytes(PING_RESPONSE));
                                goto CONT_RECEIVE;

                            case PING_RESPONSE:
                                _running = false;
                                return;
                        }
                    }

                    LastReceivedMessageTime = DateTime.Now;

                    // Copy received bytes to a new byte array
                    var receivedBytes = new byte[bytesRead];
                    Array.Copy(_buffer, receivedBytes, bytesRead);

                    // Read messages according to current wire protocol and raise MessageReceived
                    // event for all received messages
                    foreach (var message in WireProtocol.CreateMessages(receivedBytes))
                        OnMessageReceived(message, DateTime.Now);
                }
                else
                {
                    Disconnect();
                }

            CONT_RECEIVE:

                // Read more bytes if still running
                if (_running) _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, 0, ReceiveCallback, null);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        protected override Task SendMessagePublicAsync(IScsMessage message, byte priority)
        {
            EnqueueMessage(message, priority);
            return Task.CompletedTask;
        }

        /// <summary>
        /// One event-driven writer owns the socket. This removes the old 10 ms timer
        /// per connection and prevents overlapping BeginSend calls from building up
        /// under dense movement fan-out.
        /// </summary>
        private async Task RunSendPumpAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _sendSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

                    while (!cancellationToken.IsCancellationRequested &&
                           TryBuildNextBatch(out byte[] outgoingPacket))
                    {
                        await SendAllAsync(outgoingPacket, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (ObjectDisposedException)
            {
                // Normal when the channel is being torn down.
            }
            catch (SocketException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Disconnect();
                }
            }
            catch (Exception)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Disconnect();
                }
            }
        }

        private void EnqueueMessage(IScsMessage message, byte priority)
        {
            byte[] serialized = WireProtocol.GetBytes(message);
            if (serialized == null || serialized.Length == 0)
            {
                return;
            }

            if (message is ScsRawDataMessage rawMessage &&
                rawMessage.IsTransient &&
                rawMessage.TransientKey != 0)
            {
                EnqueueTransient(rawMessage.TransientKey, serialized);
                TrySignalSendPump();
                return;
            }

            if (priority > 5)
            {
                _highPriorityBuffer.Enqueue(serialized);
            }
            else
            {
                _lowPriorityBuffer.Enqueue(serialized);
            }

            TrySignalSendPump();
        }

        private void EnqueueTransient(long key, byte[] serialized)
        {
            if (_transientBuffer.TryAdd(key, serialized))
            {
                _transientKeys.Enqueue(key);
                TrimTransientStates();
                return;
            }

            // The key is already scheduled. Replace its pending payload with the
            // newest position without adding another queue node.
            _transientBuffer[key] = serialized;
        }

        private void TrimTransientStates()
        {
            while (_transientBuffer.Count > MaximumTransientStates &&
                   _transientKeys.TryDequeue(out long staleKey))
            {
                _transientBuffer.TryRemove(staleKey, out _);
            }
        }

        private void TrySignalSendPump()
        {
            try
            {
                if (_sendSignal.CurrentCount == 0)
                {
                    _sendSignal.Release();
                }
            }
            catch (SemaphoreFullException)
            {
                // A wake-up is already pending.
            }
            catch (ObjectDisposedException)
            {
                // Channel teardown raced with a final producer.
            }
        }

        private bool TryBuildNextBatch(out byte[] outgoingPacket)
        {
            if (TryBuildQueueBatch(_highPriorityBuffer, out outgoingPacket))
            {
                return true;
            }

            if (TryBuildQueueBatch(_lowPriorityBuffer, out outgoingPacket))
            {
                return true;
            }

            return TryBuildTransientBatch(out outgoingPacket);
        }

        private static bool TryBuildQueueBatch(
            ConcurrentQueue<byte[]> buffer,
            out byte[] outgoingPacket)
        {
            var messages = new List<byte[]>(MaximumPacketsPerBatch);
            int totalLength = 0;

            while (messages.Count < MaximumPacketsPerBatch &&
                   buffer.TryPeek(out byte[] nextMessage))
            {
                if (nextMessage == null || nextMessage.Length == 0)
                {
                    buffer.TryDequeue(out _);
                    continue;
                }

                if (messages.Count > 0 &&
                    totalLength + nextMessage.Length > MaximumBatchBytes)
                {
                    break;
                }

                if (!buffer.TryDequeue(out byte[] message))
                {
                    continue;
                }

                messages.Add(message);
                totalLength = checked(totalLength + message.Length);
            }

            return TryBuildPacket(messages, totalLength, out outgoingPacket);
        }

        private bool TryBuildTransientBatch(out byte[] outgoingPacket)
        {
            var messages = new List<byte[]>(MaximumPacketsPerBatch);
            int totalLength = 0;

            while (messages.Count < MaximumPacketsPerBatch &&
                   _transientKeys.TryDequeue(out long key))
            {
                if (!_transientBuffer.TryRemove(key, out byte[] message) ||
                    message == null ||
                    message.Length == 0)
                {
                    continue;
                }

                messages.Add(message);
                totalLength = checked(totalLength + message.Length);
            }

            return TryBuildPacket(messages, totalLength, out outgoingPacket);
        }

        private static bool TryBuildPacket(
            List<byte[]> messages,
            int totalLength,
            out byte[] outgoingPacket)
        {
            if (totalLength <= 0 || messages.Count == 0)
            {
                outgoingPacket = null;
                return false;
            }

            outgoingPacket = new byte[totalLength];
            int offset = 0;
            foreach (byte[] message in messages)
            {
                Buffer.BlockCopy(message, 0, outgoingPacket, offset, message.Length);
                offset += message.Length;
            }

            return true;
        }

        private async Task SendAllAsync(byte[] outgoingPacket, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (offset < outgoingPacket.Length && !cancellationToken.IsCancellationRequested)
            {
                int currentOffset = offset;
                int bytesSent = await Task.Factory.FromAsync<int>(
                        (callback, state) => _clientSocket.BeginSend(
                            outgoingPacket,
                            currentOffset,
                            outgoingPacket.Length - currentOffset,
                            SocketFlags.None,
                            callback,
                            state),
                        _clientSocket.EndSend,
                        null)
                    .ConfigureAwait(false);

                if (bytesSent <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }

                offset += bytesSent;
            }

            if (offset > 0)
            {
                LastSentMessageTime = DateTime.Now;
            }
        }

        #endregion
    }
}
