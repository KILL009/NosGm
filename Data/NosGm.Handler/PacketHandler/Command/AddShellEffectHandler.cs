using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using System;

namespace NosGm.Handler.PacketHandler.Command
{
    public class AddShellEffectHandler : IPacketHandler
    {
        #region Instantiation

        public AddShellEffectHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void AddShellEffect(AddShellEffectPacket addShellEffectPacket)
        {
            if (addShellEffectPacket != null)
            {
                try
                {
                    var instance =
                        Session.Character.Inventory.LoadBySlotAndType(addShellEffectPacket.Slot,
                            InventoryType.Equipment);
                    if (instance != null)
                        instance.ShellEffects.Add(new ShellEffectDTO
                        {
                            EffectLevel = (ShellEffectLevelType)addShellEffectPacket.EffectLevel,
                            Effect = addShellEffectPacket.Effect,
                            Value = addShellEffectPacket.Value,
                            EquipmentSerialId = instance.EquipmentSerialId
                        });
                }
                catch (Exception)
                {
                    Session.SendPacket(Session.Character.GenerateSay(AddShellEffectPacket.ReturnHelp(), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(AddShellEffectPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}