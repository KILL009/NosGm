//using System;
//using System.Linq;
//using NosGm.Packets.Packets.CommandPackets;
//using NosGm.Core;
//using NosGm.Domain;
//using NosGm.GameObject;
//using NosGm.GameObject.Extension;
//using NosGm.GameObject.Networking;

//namespace NosGm.Handler.PacketHandler.Command
//{
//    internal class RollHandler : IPacketHandler
//    {
//        #region Instantiation

//        public RollHandler(ClientSession session)
//        {
//            Session = session;
//        }

//        #endregion

//        #region Properties

//        public ClientSession Session { get; }

//        #endregion

//        #region Methods

//        public void Roll(RollPacket pa)
//        {
//            var time = Session.Character.LastRoll.AddSeconds(10);
//            if (DateTime.Now <= time)
//            {
//                return;
//            }            

//            if (ServerManager.Instance.ChannelId == 1)
//            {
//                return;
//            }

//            Session.Character.LastRoll = DateTime.Now; // ISSOU

//            var rndm = ServerManager.RandomNumber();
//            var fun = rndm >= 50 ? "Woah, you are the boss!" : "Soo gooood!";
//            var message = $"{Session.Character.Name} rolled <{rndm}>" + fun;

//            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1),
//                ReceiverType.AllExceptMe);

//            foreach (var sess in ServerManager.Instance.Sessions.Where(s => s.Account.Authority >= AuthorityType.User))

//                if (sess.HasSelectedCharacter)
//                    sess.SendPacket(
//                        sess.Character.GenerateSay($"[ROLL-Channel {Session.Character.Name}]: rolled {rndm}", 12));
//        }

//        #endregion
//    }
//}