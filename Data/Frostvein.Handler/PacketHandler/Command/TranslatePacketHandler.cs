using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Extension.Translator;

namespace Frostvein.Handler.PacketHandler.Command
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