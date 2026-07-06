//using System;
//using System.Linq;
//using Frostvein.Packets.Packets.CommandPackets;
//using Frostvein.Core;
//using Frostvein.Domain;
//using Frostvein.GameObject;
//using Frostvein.GameObject.Extension;
//using Frostvein.GameObject.Networking;

//namespace Frostvein.Handler.PacketHandler.Command
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