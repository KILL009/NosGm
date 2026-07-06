using Frostvein.SCS.Communication.Scs.Client;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using Frostvein.SCS.Communication.Scs.Server;
using System;

namespace Frostvein.SCS.Communication.Scs.Communication.EndPoints
{
    public abstract class ScsEndPoint
    {
        public static ScsEndPoint CreateEndPoint(string endPointAddress)
        {
            string str1 = !string.IsNullOrEmpty(endPointAddress) ? endPointAddress : throw new ArgumentNullException(nameof(endPointAddress));
            if (!str1.Contains("://"))
                str1 = "tcp://" + str1;
            string[] strArray = str1.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
            string str2 = strArray.Length == 2 ? strArray[0].Trim().ToLower() : throw new ApplicationException(endPointAddress + " is not a valid endpoint address.");
            string address = strArray[1].Trim();
            if (str2 == "tcp")
                return (ScsEndPoint)new ScsTcpEndPoint(address);
            throw new ApplicationException("Unsupported protocol " + str2 + " in end point " + endPointAddress);
        }

        internal abstract IScsServer CreateServer();

        internal abstract IScsClient CreateClient();
    }
}
