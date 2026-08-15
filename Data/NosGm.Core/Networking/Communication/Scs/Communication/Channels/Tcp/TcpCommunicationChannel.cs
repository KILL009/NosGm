using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints;
using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            _syncLock = new object();
            _highPriorityBuffer = new ConcurrentQueue<byte[]>();
            _lowPriorityBuffer = new ConcurrentQueue<byte[]>();
            _highPriorityBatchScratch = new byte[MAXIMUM_PACKETS_PER_BATCH][];
            _lowPriorityBatchScratch = new byte[MAXIMUM_PACKETS_PER_BATCH][];
            var cancellationToken = _sendCancellationToken.Token;

            _sendTask = StartSendingAsync(SendInterval, new TimeSpan(0, 0, 0, 0, 10), cancellationToken);
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

        private const int MAXIMUM_PACKETS_PER_BATCH = 30;

        /// <summary>
        ///     Size of the buffer that is used to receive bytes from TCP socket.
        /// </summary>
        private const int RECEIVE_BUFFER_SIZE = 4 * 1024; // 4KB

        /// <summary>
        ///     This buffer is used to receive bytes
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        ///     Socket object to send/reveice messages.
        /// </summary>
        private readonly Socket _clientSocket;

        private readonly ConcurrentQueue<byte[]> _highPriorityBuffer;

        private readonly byte[][] _highPriorityBatchScratch;

        private readonly ConcurrentQueue<byte[]> _lowPriorityBuffer;

        private readonly byte[][] _lowPriorityBatchScratch;

        private readonly ScsTcpEndPoint _remoteEndPoint;

        private readonly CancellationTokenSource _sendCancellationToken = new CancellationTokenSource();

        private readonly Task _sendTask;

        /// <summary>
        ///     This object is just used for thread synchronizing (locking).
        /// </summary>
        private readonly object _syncLock;

        private bool _disposed;

        /// <summary>
        ///     A flag to control thread's running
        /// </summary>
        private volatile bool _running;

        #endregion

        #region Methods

        public static async Task StartSendingAsync(Action action, TimeSpan period,
            CancellationToken _sendCancellationToken)
        {
            while (!_sendCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, _sendCancellationToken).ConfigureAwait(false);
                if (!_sendCancellationToken.IsCancellationRequested) action?.Invoke();
            }
        }

        public override Task ClearLowPriorityQueueAsync()
        {
            _lowPriorityBuffer.Clear();
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
                _sendCancellationToken.Cancel();
                if (_clientSocket.Connected) _clientSocket.Close();

                _clientSocket.Dispose();
            }
            catch (Exception)
            {
                // do nothing
            }
            finally
            {
                _sendCancellationToken.Dispose();
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

        public void SendInterval()
        {
            try
            {
                if (WireProtocol != null)
                {
                    SendByPriority(_highPriorityBuffer, _highPriorityBatchScratch);
                    SendByPriority(_lowPriorityBuffer, _lowPriorityBatchScratch);
                }
            }
            catch (Exception)
            {
                // disconnect
            }

            if (!_clientSocket.Connected)
            {
                // do nothing
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
                _sendCancellationToken.Dispose();
            }
        }

        /// <summary>
        ///     Sends a message to the remote application.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        /// <param name="priority">Priority of message to send</param>
        protected override void SendMessagePublic(IScsMessage message, byte priority)
        {
            if (priority > 5)
                _highPriorityBuffer.Enqueue(WireProtocol.GetBytes(message));
            else
                _lowPriorityBuffer.Enqueue(WireProtocol.GetBytes(message));
        }

        /// <summary>
        ///     Starts the thread to receive messages from socket.
        /// </summary>
        protected override void StartPublic()
        {
            _running = true;
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, 0, ReceiveCallback, null);
        }

        private static void SendCallback(IAsyncResult result)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (Socket)result.AsyncState;

                if (!client.Connected) return;

                // Complete sending the data to the remote device.
                var bytesSent = client.EndSend(result);
            }
            catch (Exception)
            {
                // disconnect
            }
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
                var bytesRead = -1;

                // Get received bytes count
                bytesRead = _clientSocket.EndReceive(result);

                if (bytesRead > 0)
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
                    //Logger.Info(Language.Instance.GetMessageFromKey("CLIENT_DISCONNECTED"));
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

        private void SendByPriority(ConcurrentQueue<byte[]> buffer, byte[][] batchScratch)
        {
            int messageCount = 0;
            int totalLength = 0;

            for (int index = 0; index < MAXIMUM_PACKETS_PER_BATCH; index++)
            {
                if (!buffer.TryDequeue(out byte[] message) || message == null || message.Length == 0)
                {
                    break;
                }

                batchScratch[messageCount++] = message;
                totalLength = checked(totalLength + message.Length);
            }

            if (totalLength == 0)
            {
                return;
            }

            var outgoingPacket = new byte[totalLength];
            int offset = 0;
            for (int index = 0; index < messageCount; index++)
            {
                byte[] message = batchScratch[index];
                Buffer.BlockCopy(message, 0, outgoingPacket, offset, message.Length);
                offset += message.Length;
                batchScratch[index] = null;
            }

            _clientSocket.BeginSend(
                outgoingPacket,
                0,
                outgoingPacket.Length,
                SocketFlags.None,
                SendCallback,
                _clientSocket);
        }

        protected override Task SendMessagePublicAsync(IScsMessage message, byte priority)
        {
            if (priority > 5)
            {
                _highPriorityBuffer.Enqueue(WireProtocol.GetBytes(message));
            }
            else
            {
                _lowPriorityBuffer.Enqueue(WireProtocol.GetBytes(message));
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
