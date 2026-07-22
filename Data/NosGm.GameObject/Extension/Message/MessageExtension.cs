using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Extension.Message
{
    public static class MessageExtension
    {
        public static void SendRaidDamage(ClientSession session, byte type, EventContainer evt)
        {
            try
            {
                switch (type)
                {
                    case 1:
                        try
                        {
                            string formattedDamage = session.Character.DamageInRaid.ToString("N0");
                            if (formattedDamage == null)
                            {
                                Console.WriteLine("FormattedDamage was null");
                                return;
                            }
                            SendBubble(session, $"Damage: " + formattedDamage);
                            SendGreen(session, $"Damage: " + formattedDamage);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;

                    case 2:
                        try
                        {
                            var owner = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character.Group?.Raid?.InstanceBag.CreatorId == s.Character.CharacterId)?.Character;
                            var group = owner?.Group;
                            var groupMembers = new ClientSession[group.SessionCount];
                            group.Sessions.CopyTo(groupMembers);
                            foreach (var groupMember in groupMembers)
                            {
                                string formattedDamageAll = groupMember.Character.DamageInRaid.ToString("N0");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void SendGreen(ClientSession session, string Message)
        {
            session.SendPacket($"say 1 {session.Character.CharacterId} 12 {Message}");
        }

        public static void SendYellow(ClientSession session, string Message)
        {
            session.SendPacket($"say 1 {session.Character.CharacterId} 10 {Message}");
        }

        public static void SendRed(ClientSession session, string Message)
        {
            session.SendPacket($"say 1 {session.Character.CharacterId} 11 {Message}");
        }

        public static void SendGrey(ClientSession session, string Message)
        {
            session.SendPacket($"say 1 {session.Character.CharacterId} 13 {Message}");
        }

        public static void SendBubble(ClientSession session, string Message)
        {
            session.SendPacket($"say 1 {session.Character.CharacterId} 1 {Message}");
        }

        public static void SendInfo(ClientSession session, string Message)
        {
            session.SendPacket($"info {Message}");
        }

        public static void SendModal(ClientSession session, string Message)
        {
            session.SendPacket($"modal 1 {Message}");
        }

        public static void SendHero(ClientSession session, string Message)
        {
            session.SendPacket($"msg 5 {Message}");
        }

        public static void SendHeader(ClientSession session, string Message)
        {
            session.SendPacket($"msg 2 {Message}");
        }

        public static void SendSmallHeader(ClientSession session, string Message)
        {
            session.SendPacket($"msg 3 {Message}");
        }
    }
}
