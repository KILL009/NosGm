using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Custom
{
    public class TemplatePacketHandler : IPacketHandler
    {
        #region Instantiation

        public TemplatePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        #endregion
    }
}