using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.DAL.EF.Interceptors
{
    public static class SlowQueryLogWriter
    {
        private static readonly BlockingCollection<string> _queue = new BlockingCollection<string>(1000);
        private static int _droppedMessages = 0;
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static SlowQueryLogWriter()
        {
            Task.Factory.StartNew(ProcessQueue, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public static void Log(string message)
        {
            if (!_queue.TryAdd(message))
            {
                Interlocked.Increment(ref _droppedMessages);
            }
        }

        private static void ProcessQueue()
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                foreach (var message in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                        string path = Path.Combine(logDir, $"slow_queries-{date}.log");
                        string logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
                        File.AppendAllText(path, logLine);
                    }
                    catch
                    {
                        // Fallback silently if file is locked or cannot write
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown
            }
        }
    }
}
