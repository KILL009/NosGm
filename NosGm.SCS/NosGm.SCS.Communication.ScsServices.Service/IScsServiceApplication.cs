using System;

namespace NosGm.SCS.Communication.ScsServices.Service
{
    public interface IScsServiceApplication
    {
        event EventHandler<ServiceClientEventArgs> ClientConnected;

        event EventHandler<ServiceClientEventArgs> ClientDisconnected;

        void Start();

        void Stop();

        void AddService<TServiceInterface, TServiceClass>(TServiceClass service)
          where TServiceInterface : class
          where TServiceClass : ScsService, TServiceInterface;

        bool RemoveService<TServiceInterface>() where TServiceInterface : class;
    }
}
