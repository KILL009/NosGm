using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
{
    public class SetPerfectionHandler : IPacketHandler
    {
        #region Instantiation

        public SetPerfectionHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SetPerfection(SetPerfectionPacket setPerfectionPacket)
        {
            if (setPerfectionPacket != null)
            {
                //Session.AddLogsCmd(setPerfectionPacket);
                if (setPerfectionPacket.Slot >= 0)
                {
                    var specialistInstance =
                        Session.Character.Inventory.LoadBySlotAndType(setPerfectionPacket.Slot, 0);

                    if (specialistInstance != null)
                    {
                        switch (setPerfectionPacket.Type)
                        {
                            case 0:
                                specialistInstance.SpStoneUpgrade = setPerfectionPacket.Value;
                                break;

                            case 1:
                                specialistInstance.SpDamage = setPerfectionPacket.Value;
                                break;

                            case 2:
                                specialistInstance.SpDefence = setPerfectionPacket.Value;
                                break;

                            case 3:
                                specialistInstance.SpElement = setPerfectionPacket.Value;
                                break;

                            case 4:
                                specialistInstance.SpHP = setPerfectionPacket.Value;
                                break;

                            case 5:
                                specialistInstance.SpFire = setPerfectionPacket.Value;
                                break;

                            case 6:
                                specialistInstance.SpWater = setPerfectionPacket.Value;
                                break;

                            case 7:
                                specialistInstance.SpLight = setPerfectionPacket.Value;
                                break;

                            case 8:
                                specialistInstance.SpDark = setPerfectionPacket.Value;
                                break;

                            default:
                                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(),
                                    10));
                                break;
                        }
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UpgradeCommandPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}