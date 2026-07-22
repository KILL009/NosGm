using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosGm.Handler.PacketHandler.Command
{
    public class HelpHandler : IPacketHandler
    {
        #region Instantiation

        public HelpHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Command(HelpPacket helpPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("-------------Commands Info-------------", 11));
            List<string> commandHelpMessages = GetCommandHelpMessages();
            foreach (string message in commandHelpMessages)
            {
                Session.SendPacket(Session.Character.GenerateSay(message, 12));
            }
            Session.SendPacket(Session.Character.GenerateSay("-----------------------------------------------", 11));
        }

        private List<string> GetCommandHelpMessages()
        {
            List<string> messages = new List<string>();
            List<Type> commandTypes = GetCommandTypes();
            foreach (Type type in commandTypes)
            {
                string message = GetHelpMessage(type);

                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message);
                }
            }
            return messages;
        }

        private List<Type> GetCommandTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes().Where(type => type.IsClass && type.Namespace == "NosGm.Packets.Packets.CommandPackets"))
                .OrderBy(type => type.Name)
                .ToList();
        }

        private string GetHelpMessage(Type type)
        {
            object classInstance = Activator.CreateInstance(type);
            MethodInfo method = type.GetMethod("ReturnHelp");
            return method?.Invoke(classInstance, null)?.ToString();
        }

        #endregion
    }
}