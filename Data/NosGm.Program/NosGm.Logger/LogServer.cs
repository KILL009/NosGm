using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Configuration;
using NosGm.Domain;
using NosGm.LogServer.MongoDB;

namespace NosGm.LoggerService
{
    public static class LogServer
    {
        public static class Logger
        {
            private const int QueueCapacity = LogPipelineMonitor.DefaultUdpQueueCapacity;
            private static readonly string IPAdress = "127.0.0.1";
            private static readonly int Port = 1912;
            private static readonly BlockingCollection<QueuedUdpMessage> SendQueue =
                new BlockingCollection<QueuedUdpMessage>(
                    new ConcurrentQueue<QueuedUdpMessage>(), QueueCapacity);
            private static readonly UdpClient Client = new UdpClient();
            private static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(IPAdress), Port);
            private static readonly object LoadOutputSync = new object();
            private static readonly Thread SenderThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "NosGM-Log-UDP"
            };

            private static long _droppedMessages;

            static Logger()
            {
                LogPipelineMonitor.UpdateUdpQueue(0, QueueCapacity);
                SenderThread.Start();
            }

            public static long DroppedMessages => Interlocked.Read(ref _droppedMessages);

            public static async Task LogAsync(string input, LogType logType,
                [CallerMemberName] string caller = "", [CallerFilePath] string file = "",
                [CallerLineNumber] int line = 0)
            {
                try
                {
                    if (logType == LogType.ERROR)
                    {
                        await LogService.Generate(input, logType).ConfigureAwait(false);
                    }

                    string time = DateTime.Now.ToString($"[HH:mm:ss][{logType}]");
                    Enqueue($"{time} {input}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            public static Task UpdateLoadOutput(string input, LogType logType)
            {
                lock (LoadOutputSync)
                {
                    LogConfiguration.LoadOutput += $"{input} | ";
                }

                string time = DateTime.Now.ToString("[HH:mm:ss][LOAD]");
                Enqueue($"{time} {input}");
                return Task.CompletedTask;
            }

            public static string NameOfCallingClass()
            {
                string fullName;
                Type declaringType;
                int skipFrames = 2;
                do
                {
                    MethodBase method = new StackFrame(skipFrames, false).GetMethod();
                    declaringType = method.DeclaringType;
                    if (declaringType == null)
                    {
                        return method.Name;
                    }
                    skipFrames++;
                    fullName = declaringType.FullName;
                }
                while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

                return fullName;
            }

            private static void Enqueue(string message)
            {
                LogPipelineOperation operation = LogPipelineMonitor.CurrentOperation;
                var queued = new QueuedUdpMessage(message, operation);
                if (SendQueue.TryAdd(queued))
                {
                    LogPipelineMonitor.RecordUdpEnqueued(operation, SendQueue.Count, QueueCapacity);
                    return;
                }

                Interlocked.Increment(ref _droppedMessages);
                LogPipelineMonitor.RecordUdpDropped(operation, SendQueue.Count, QueueCapacity);
            }

            private static void ProcessQueue()
            {
                foreach (QueuedUdpMessage queued in SendQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(queued.Message);
                        Client.Send(buffer, buffer.Length, EndPoint);
                        LogPipelineMonitor.RecordUdpSent(
                            queued.Operation,
                            SendQueue.Count,
                            QueueCapacity);
                    }
                    catch
                    {
                        Interlocked.Increment(ref _droppedMessages);
                        LogPipelineMonitor.RecordUdpError(
                            queued.Operation,
                            SendQueue.Count,
                            QueueCapacity);
                        LogPipelineMonitor.RecordUdpDropped(
                            queued.Operation,
                            SendQueue.Count,
                            QueueCapacity);
                    }
                }
            }

            private sealed class QueuedUdpMessage
            {
                public QueuedUdpMessage(string message, LogPipelineOperation operation)
                {
                    Message = message;
                    Operation = operation;
                }

                public string Message { get; }

                public LogPipelineOperation Operation { get; }
            }
        }
    }
}
