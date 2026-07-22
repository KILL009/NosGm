using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints;
using NosGm.Core.Networking.Communication.Scs.Server;

namespace NosGm.Core.Networking.Communication.ScsServices.Service
{
    /// <summary>
    ///     This class is used to build ScsService applications.
    /// </summary>
    public static class ScsServiceBuilder
    {
        #region Methods

        /// <summary>
        ///     Creates a new SCS Service application using an EndPoint.
        /// </summary>
        /// <param name="endPoint">EndPoint that represents address of the service</param>
        /// <returns>Created SCS service application</returns>
        public static IScsServiceApplication CreateService(ScsEndPoint endPoint) => new ScsServiceApplication(ScsServerFactory.CreateServer(endPoint));

        #endregion
    }
}