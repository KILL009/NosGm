using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Extension.Translator;

namespace NosGm.Handler.PacketHandler.Command
{
    public class TranslatePacketHandler : IPacketHandler
    {
        #region Instantiation

        public TranslatePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Translate(TranslatePacket translatePacket)
        {
            string Message = translatePacket.Message;
            string From = translatePacket.From;
            string To = translatePacket.To;
            MessageExtension.SendGrey(Session, $"Text: {translatePacket.Message}\nFrom: {From.ToUpper()} | To: {To.ToUpper()}\nResult:");
            TranslatorExtension.TranslateCommand(Session, Message, From, To);
        }

        #endregion
    }
}