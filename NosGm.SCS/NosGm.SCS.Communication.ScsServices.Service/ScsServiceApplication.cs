using NosGm.SCS.Collections;
using NosGm.SCS.Communication.Scs.Communication.Messages;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using NosGm.SCS.Communication.Scs.Server;
using NosGm.SCS.Communication.ScsServices.Communication.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NosGm.SCS.Communication.ScsServices.Service
{
    internal class ScsServiceApplication : IScsServiceApplication
    {
        private const int MaximumConcurrentServiceCallsPerClient = 8;

        private readonly IScsServer _scsServer;
        private readonly ThreadSafeSortedList<string, ScsServiceApplication.ServiceObject> _serviceObjects;
        private readonly ThreadSafeSortedList<long, IScsServiceClient> _serviceClients;

        [CompilerGenerated]
        public event EventHandler<ServiceClientEventArgs> ClientConnected;

        [CompilerGenerated]
        public event EventHandler<ServiceClientEventArgs> ClientDisconnected;

        public ScsServiceApplication(IScsServer scsServer)
        {
            this._scsServer = scsServer != null ? scsServer : throw new ArgumentNullException(nameof(scsServer));
            this._scsServer.ClientConnected += new EventHandler<ServerClientEventArgs>(this.ScsServer_ClientConnected);
            this._scsServer.ClientDisconnected += new EventHandler<ServerClientEventArgs>(this.ScsServer_ClientDisconnected);
            this._serviceObjects = new ThreadSafeSortedList<string, ScsServiceApplication.ServiceObject>();
            this._serviceClients = new ThreadSafeSortedList<long, IScsServiceClient>();
        }

        public void Start() => this._scsServer.Start();

        public void Stop() => this._scsServer.Stop();

        public void AddService<TServiceInterface, TServiceClass>(TServiceClass service)
          where TServiceInterface : class
          where TServiceClass : ScsService, TServiceInterface
        {
            if ((object)service == null)
                throw new ArgumentNullException(nameof(service));
            Type serviceInterfaceType = typeof(TServiceInterface);
            if (this._serviceObjects[serviceInterfaceType.Name] != null)
                throw new Exception("Service '" + serviceInterfaceType.Name + "' is already added before.");
            this._serviceObjects[serviceInterfaceType.Name] = new ScsServiceApplication.ServiceObject(serviceInterfaceType, (ScsService)service);
        }

        public bool RemoveService<TServiceInterface>() where TServiceInterface : class
        {
            return this._serviceObjects.Remove(typeof(TServiceInterface).Name);
        }

        private void ScsServer_ClientConnected(object sender, ServerClientEventArgs e)
        {
            RequestReplyMessenger<IScsServerClient> requestReplyMessenger =
                new RequestReplyMessenger<IScsServerClient>(
                    e.Client,
                    MaximumConcurrentServiceCallsPerClient);
            requestReplyMessenger.MessageReceived += new EventHandler<MessageEventArgs>(this.Client_MessageReceived);
            requestReplyMessenger.Start();
            IScsServiceClient serviceClient = ScsServiceClientFactory.CreateServiceClient(e.Client, requestReplyMessenger);
            this._serviceClients[serviceClient.ClientId] = serviceClient;
            this.OnClientConnected(serviceClient);
        }

        private void ScsServer_ClientDisconnected(object sender, ServerClientEventArgs e)
        {
            IScsServiceClient serviceClient = this._serviceClients[e.Client.ClientId];
            if (serviceClient == null)
                return;
            this._serviceClients.Remove(e.Client.ClientId);
            this.OnClientDisconnected(serviceClient);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            RequestReplyMessenger<IScsServerClient> client = (RequestReplyMessenger<IScsServerClient>)sender;
            if (!(e.Message is ScsRemoteInvokeMessage message))
                return;
            try
            {
                IScsServiceClient serviceClient = this._serviceClients[client.Messenger.ClientId];
                if (serviceClient == null)
                {
                    client.Messenger.Disconnect();
                }
                else
                {
                    ScsServiceApplication.ServiceObject serviceObject = this._serviceObjects[message.ServiceClassName];
                    if (serviceObject == null)
                    {
                        ScsServiceApplication.SendInvokeResponse((IMessenger)client, (IScsMessage)message, (object)null, new ScsRemoteException("There is no service with name '" + message.ServiceClassName + "'"));
                    }
                    else
                    {
                        try
                        {
                            serviceObject.Service.CurrentClient = serviceClient;
                            object returnValue;
                            try
                            {
                                returnValue = serviceObject.InvokeMethod(message.MethodName, message.Parameters);
                            }
                            finally
                            {
                                serviceObject.Service.CurrentClient = (IScsServiceClient)null;
                            }
                            ScsServiceApplication.SendInvokeResponse((IMessenger)client, (IScsMessage)message, returnValue, (ScsRemoteException)null);
                        }
                        catch (TargetInvocationException ex)
                        {
                            Exception innerException = ex.InnerException;
                            ScsServiceApplication.SendInvokeResponse((IMessenger)client, (IScsMessage)message, (object)null, new ScsRemoteException(innerException.Message + Environment.NewLine + "Service Version: " + serviceObject.ServiceAttribute.Version, innerException));
                        }
                        catch (Exception ex)
                        {
                            ScsServiceApplication.SendInvokeResponse((IMessenger)client, (IScsMessage)message, (object)null, new ScsRemoteException(ex.Message + Environment.NewLine + "Service Version: " + serviceObject.ServiceAttribute.Version, ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ScsServiceApplication.SendInvokeResponse((IMessenger)client, (IScsMessage)message, (object)null, new ScsRemoteException("An error occured during remote service method call.", ex));
            }
        }

        private static void SendInvokeResponse(
          IMessenger client,
          IScsMessage requestMessage,
          object returnValue,
          ScsRemoteException exception)
        {
            try
            {
                IMessenger messenger = client;
                ScsRemoteInvokeReturnMessage invokeReturnMessage = new ScsRemoteInvokeReturnMessage();
                invokeReturnMessage.RepliedMessageId = requestMessage.MessageId;
                invokeReturnMessage.ReturnValue = returnValue;
                invokeReturnMessage.RemoteException = exception;
                ScsRemoteInvokeReturnMessage message = invokeReturnMessage;
                messenger.SendMessage((IScsMessage)message);
            }
            catch
            {
            }
        }

        private void OnClientConnected(IScsServiceClient client)
        {
            EventHandler<ServiceClientEventArgs> clientConnected = this.ClientConnected;
            if (clientConnected == null)
                return;
            clientConnected((object)this, new ServiceClientEventArgs(client));
        }

        private void OnClientDisconnected(IScsServiceClient client)
        {
            EventHandler<ServiceClientEventArgs> clientDisconnected = this.ClientDisconnected;
            if (clientDisconnected == null)
                return;
            clientDisconnected((object)this, new ServiceClientEventArgs(client));
        }

        private sealed class ServiceObject
        {
            private readonly SortedList<string, MethodInfo> _methods;

            public ScsService Service { get; private set; }

            public ScsServiceAttribute ServiceAttribute { get; private set; }

            public ServiceObject(Type serviceInterfaceType, ScsService service)
            {
                this.Service = service;
                object[] customAttributes = serviceInterfaceType.GetCustomAttributes(typeof(ScsServiceAttribute), true);
                this.ServiceAttribute = customAttributes.Length != 0 ? customAttributes[0] as ScsServiceAttribute : throw new Exception("Service interface (" + serviceInterfaceType.Name + ") must has ScsService attribute.");
                this._methods = new SortedList<string, MethodInfo>();
                foreach (MethodInfo method in serviceInterfaceType.GetMethods())
                    this._methods.Add(method.Name, method);
            }

            public object InvokeMethod(string methodName, params object[] parameters)
            {
                if (!this._methods.ContainsKey(methodName))
                    throw new Exception("There is not a method with name '" + methodName + "' in service class.");
                return this._methods[methodName].Invoke((object)this.Service, parameters);
            }
        }
    }
}
