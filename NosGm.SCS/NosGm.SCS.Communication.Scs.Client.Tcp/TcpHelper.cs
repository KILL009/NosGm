using System;
using System.Net;
using System.Net.Sockets;

namespace NosGm.SCS.Communication.Scs.Client.Tcp
{
    internal static class TcpHelper
    {
        public static Socket ConnectToServer(EndPoint endPoint, int timeoutMs)
        {
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                server.Blocking = false;
                server.Connect(endPoint);
                server.Blocking = true;
                return server;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode != 10035)
                {
                    server.Close();
                    throw;
                }
                else
                {
                    if (!server.Poll(timeoutMs * 1000, SelectMode.SelectWrite))
                    {
                        server.Close();
                        throw new TimeoutException("The host failed to connect. Timeout occured.");
                    }
                    server.Blocking = true;
                    return server;
                }
            }
        }
    }
}
