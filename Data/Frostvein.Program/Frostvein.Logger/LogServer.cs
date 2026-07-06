using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Frostvein.Domain;
using Frostvein.LogServer.MongoDB;
using Frostvein.Configuration;

namespace Frostvein.LoggerService
{
    public static class LogServer
    {
        public static class Logger
        {
            private static string IPAdress { get; set; } = "127.0.0.1";
            private static int Port { get; set; } = 1912;

            public static async Task LogAsync(string Input, LogType LogType, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            {
                try
                {
                    switch (LogType)
                    {
                        //In case something should be handled other than sending a Log to the Server
                        case LogType.ERROR:
                            await LogService.Generate(Input, LogType);
                            break;
                        case LogType.WARNING:
                        case LogType.INFO:
                        case LogType.Character:
                        case LogType.CharacterAction:
                        case LogType.CharacterCommand:
                        case LogType.CharacterStaffCommand:
                        case LogType.Trade:
                        case LogType.Bet:
                        case LogType.UpgradeEquipment:
                        case LogType.UpgradeSpecialistCard:
                        case LogType.UpgradeSpecialistCardPerfection:
                        case LogType.SumResistance:
                        case LogType.BazaarBuy:
                        case LogType.BazaarSell:
                        case LogType.BazaarMod:
                        case LogType.Ban:
                        case LogType.Kick:
                        case LogType.Mute:
                        case LogType.Exploit:
                            break;
                    }
                    string Time = DateTime.Now.ToString($"[HH:mm:ss][{LogType}]");
                    await SendInfoAsync($"{Time} {Input}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                
            }

            public static async Task UpdateLoadOutput(string Input, LogType logType)
            {
                await Task.Run(() => LogConfiguration.LoadOutput += $"{Input} | ");
                string Time = DateTime.Now.ToString($"[HH:mm:ss][LOAD]");
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
                await Task.Run(() => s.SendTo(sendbuf, ep));
            }
        }
    }
}
