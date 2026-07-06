using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class BuffHandler : IPacketHandler
    {
        #region Instantiation

        public BuffHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Buff(BuffPacket buffPacket)
        {
            if (buffPacket != null)
            {
                var buff = new Buff(buffPacket.CardId, buffPacket.Level ?? (byte)1);
                Session.Character.AddBuff(buff, Session.Character.BattleEntity);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BuffPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}