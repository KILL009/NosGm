using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;


namespace Frostvein.LogServer
{
    public class Program
    {
        private const int _port = 1912;
        private static Thread FormThread = new Thread(new ThreadStart(StartListener));
        private static void StartListener()
        {
            UdpClient listener = new UdpClient(_port);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _port);
            try
            {
                while (true)
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    var text = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine(text);
                }
            }
            catch (SocketException)
            {
            }
        }

        public static void Main(string[] args)
        {
            Console.Title = "Frostvein - Log Server";
            Console.ForegroundColor = ConsoleColor.Green;
            FormThread.IsBackground = true;
            FormThread.Start();
            Console.ReadKey();
        }
    }
}
