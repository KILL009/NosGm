using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Threading;
using System;

namespace NosGm.SCS.Communication.Scs.Client
{
    public class ClientReConnecter : IDisposable
    {
        private readonly IConnectableClient _client;
        private readonly Timer _reconnectTimer;
        private volatile bool _disposed;

        public int ReConnectCheckPeriod
        {
            get => this._reconnectTimer.Period;
            set => this._reconnectTimer.Period = value;
        }

        public ClientReConnecter(IConnectableClient client)
        {
            this._client = client != null ? client : throw new ArgumentNullException(nameof(client));
            this._client.Disconnected += new EventHandler(this.Client_Disconnected);
            this._reconnectTimer = new Timer(20000);
            this._reconnectTimer.Elapsed += new EventHandler(this.ReconnectTimer_Elapsed);
            this._reconnectTimer.Start();
        }

        public void Dispose()
        {
            if (this._disposed)
                return;
            this._disposed = true;
            this._client.Disconnected -= new EventHandler(this.Client_Disconnected);
            this._reconnectTimer.Stop();
        }

        private void Client_Disconnected(object sender, EventArgs e) => this._reconnectTimer.Start();

        private void ReconnectTimer_Elapsed(object sender, EventArgs e)
        {
            if (!this._disposed)
            {
                if (this._client.CommunicationState != CommunicationStates.Connected)
                {
                    try
                    {
                        this._client.Connect();
                        this._reconnectTimer.Stop();
                        return;
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            this._reconnectTimer.Stop();
        }
    }
}
