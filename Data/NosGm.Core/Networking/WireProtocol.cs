using NosGm.Core.ExceptionExtensions;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using NosGm.Core.Networking.Communication.Scs.Communication.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NosGm.Core
{
    public class WireProtocol : IScsWireProtocol, IDisposable
    {
        #region Instantiation

        public WireProtocol()
        {
            _receiveMemoryStream = new MemoryStream();
        }

        #endregion

        #region Members

        /// <summary>
        ///     Maximum length of one raw message accepted from the network layer.
        /// </summary>
        private const int MaximumMessageLength = 4096;

        private bool _disposed;

        /// <summary>
        ///     Collects bytes supplied by the network layer for the current raw message.
        /// </summary>
        private MemoryStream _receiveMemoryStream;

        #endregion

        #region Methods

        public IEnumerable<IScsMessage> CreateMessages(byte[] receivedBytes)
        {
            ThrowIfDisposed();

            if (receivedBytes == null)
            {
                throw new ArgumentNullException(nameof(receivedBytes));
            }

            if (receivedBytes.Length == 0)
            {
                return Array.Empty<IScsMessage>();
            }

            long pendingLength = _receiveMemoryStream.Length + receivedBytes.Length;
            if (pendingLength > MaximumMessageLength)
            {
                ResetReceiveBuffer();
                throw new CommunicationException(
                    $"Message is too big ({pendingLength} bytes). Max allowed length is {MaximumMessageLength} bytes.");
            }

            _receiveMemoryStream.Write(receivedBytes, 0, receivedBytes.Length);

            var messages = new List<IScsMessage>();
            while (ReadSingleMessage(messages))
            {
            }

            return messages;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        public byte[] GetBytes(IScsMessage message)
        {
            ThrowIfDisposed();

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message is ScsTextMessage textMessage)
            {
                return Encoding.Default.GetBytes(textMessage.Text);
            }

            if (message is ScsRawDataMessage rawDataMessage)
            {
                return rawDataMessage.MessageData;
            }

            throw new ArgumentException($"Unsupported message type: {message.GetType().FullName}", nameof(message));
        }

        public void Reset()
        {
            ThrowIfDisposed();
            ResetReceiveBuffer();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _receiveMemoryStream?.Dispose();
                _receiveMemoryStream = null;
            }
        }

        private static byte[] ReadByteArray(Stream stream, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int read = stream.Read(buffer, offset, length - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException("Can not read from stream! Input stream is closed.");
                }

                offset += read;
            }

            return buffer;
        }

        private bool ReadSingleMessage(ICollection<IScsMessage> messages)
        {
            _receiveMemoryStream.Position = 0;

            long pendingLength = _receiveMemoryStream.Length;
            if (pendingLength == 0)
            {
                return false;
            }

            if (pendingLength > MaximumMessageLength)
            {
                ResetReceiveBuffer();
                throw new CommunicationException(
                    $"Message is too big ({pendingLength} bytes). Max allowed length is {MaximumMessageLength} bytes.");
            }

            int frameLength = checked((int)pendingLength);
            byte[] serializedMessageBytes = ReadByteArray(_receiveMemoryStream, frameLength);
            messages.Add(new ScsRawDataMessage(serializedMessageBytes));

            ResetReceiveBuffer();
            return false;
        }

        private void ResetReceiveBuffer()
        {
            MemoryStream previous = _receiveMemoryStream;
            _receiveMemoryStream = new MemoryStream();
            previous?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WireProtocol));
            }
        }

        #endregion
    }
}
