using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.GameObject;
using NosGm.Master.Library.Client;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class KickSessionHandler : IPacketHandler
    {
        #region Instantiation

        public KickSessionHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task KickSession(KickSessionPacket kickSessionPacket)
        {
            if (kickSessionPacket != null)
            {
                //Session.AddLogsCmd(kickSessionPacket);
                if (kickSessionPacket.SessionId.HasValue) //if you set the sessionId, remove account verification
                    kickSessionPacket.AccountName = "";

                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                var account = DAOFactory.AccountDAO.LoadByName(kickSessionPacket.AccountName);
                CommunicationServiceClient.Instance.KickSession(account?.AccountId, kickSessionPacket.SessionId);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KickSessionPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}