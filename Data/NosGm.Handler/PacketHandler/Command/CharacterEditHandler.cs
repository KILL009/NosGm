using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Networking;
using System;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class CharacterEditHandler : IPacketHandler
    {
        #region Instantiation

        public CharacterEditHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task CharacterEdit(CharacterEditPacket characterEditPacket)
        {
            if (characterEditPacket != null)
            {
                if (characterEditPacket.Property != null && !string.IsNullOrEmpty(characterEditPacket.Data))
                {
                    var propertyInfo = Session.Character.GetType().GetProperty(characterEditPacket.Property);
                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(Session.Character,
                            Convert.ChangeType(characterEditPacket.Data, propertyInfo.PropertyType));
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId);
                        Session.Character.Event.EmitEvent(new CharacterSaveEvent());
                        MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(CharacterEditPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}