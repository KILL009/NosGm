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

namespace NosTale.ServiceManager.LogServer
{
    public static class LogServer
    {
        public class Log
        {
            private const int QueueCapacity = 4096;
            private static readonly string IPAdress = "134.255.235.74";
            private static readonly int Port = 1912;
            private static readonly BlockingCollection<string> SendQueue =
                new BlockingCollection<string>(new ConcurrentQueue<string>(), QueueCapacity);
            private static readonly UdpClient Client = new UdpClient();
            private static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(IPAdress), Port);
            private static readonly Thread SenderThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "NosGM-ServiceManager-UDP"
            };

            private static long _droppedMessages;

            static Log()
            {
                SenderThread.Start();
            }

            public static long DroppedMessages => Interlocked.Read(ref _droppedMessages);

            public static Task LogAsync(string input, [CallerMemberName] string caller = "",
                [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            {
                string time = DateTime.Now.ToString("[HH:mm:ss]");
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
                if (!SendQueue.TryAdd(message))
                {
                    Interlocked.Increment(ref _droppedMessages);
                }
            }

            private static void ProcessQueue()
            {
                foreach (string message in SendQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        Client.Send(buffer, buffer.Length, EndPoint);
                    }
                    catch
                    {
                        Interlocked.Increment(ref _droppedMessages);
                    }
                }
            }
        }
    }
}
