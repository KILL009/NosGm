using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;

namespace NosTale.ServiceManager.LogServer
{
    public static class LogServer
    {
        public class Log
        {
            private static string IPAdress { get; set; } = "134.255.235.74";
            private static int Port { get; set; } = 1912;

            public static async Task LogAsync(string Input, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            {
                string Time = DateTime.Now.ToString("[HH:mm:ss]");
                await SendInfoAsync($"{Time} {Input}");
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

            private static void SendInfo(string message)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPAddress broadcast = IPAddress.Parse(IPAdress);
                byte[] sendbuf = Encoding.ASCII.GetBytes(message);
                IPEndPoint ep = new IPEndPoint(broadcast, Port);
                s.SendTo(sendbuf, ep);
            }

            private static async Task SendInfoAsync(string message)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPAddress broadcast = IPAddress.Parse(IPAdress);
                byte[] sendbuf = Encoding.ASCII.GetBytes(message);
                IPEndPoint ep = new IPEndPoint(broadcast, Port);
                s.SendTo(sendbuf, ep);
            }
        }
    }
}
