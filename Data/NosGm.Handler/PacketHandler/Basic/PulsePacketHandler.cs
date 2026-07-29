using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.Master.Library.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class PulsePacketHandler : IPacketHandler
    {
        #region Instantiation

        public PulsePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Pulse(PulsePacket pulsepacket)
        {
            if ((Session.Character.LastPulse.AddMilliseconds(80000) >= DateTime.Now && DateTime.Now >= Session.Character.LastPulse.AddMilliseconds(40000)) || Debugger.IsAttached)
            {
                Session.Character.LastPulse = DateTime.Now;
            }
            else
            {
                Session.Disconnect();
            }

            Session.Character.MuteMessage();
            Session.Character.DeleteTimeout();
            CommunicationServiceClient.Instance.PulseAccount(
                Session.Account.AccountId,
                Session.SessionId);
        }

        #endregion
    }
}
