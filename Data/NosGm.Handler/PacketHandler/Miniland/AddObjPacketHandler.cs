using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Miniland
{
    public class AddObjPacketHandler : IPacketHandler
    {
        #region Instantiation

        public AddObjPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MinilandAddObject(AddObjPacket addObjPacket)
        {
            var minilandobject =
                Session.Character.Inventory.LoadBySlotAndType(addObjPacket.Slot, InventoryType.Miniland);
            if (minilandobject != null)
            {
                if (Session.Character.MinilandObjects.All(s => s.ItemInstanceId != minilandobject.Id))
                {
                    if (Session.Character.MinilandState == MinilandState.Lock)
                    {
                        var minilandobj = new MinilandObject
                        {
                            CharacterId = Session.Character.CharacterId,
                            ItemInstance = minilandobject,
                            ItemInstanceId = minilandobject.Id,
                            MapX = addObjPacket.PositionX,
                            MapY = addObjPacket.PositionY,
                            Level1BoxAmount = 0,
                            Level2BoxAmount = 0,
                            Level3BoxAmount = 0,
                            Level4BoxAmount = 0,
                            Level5BoxAmount = 0
                        };

                        if (minilandobject.Item.ItemType == ItemType.House)
                        {
                            switch (minilandobject.Item.ItemSubType)
                            {
                                case 2:
                                    minilandobj.MapX = 31;
                                    minilandobj.MapY = 3;
                                    break;

                                case 0:
                                    minilandobj.MapX = 24;
                                    minilandobj.MapY = 7;
                                    break;

                                case 1:
                                    minilandobj.MapX = 21;
                                    minilandobj.MapY = 4;
                                    break;
                            }

                            var min = Session.Character.MinilandObjects.Find(s =>
                                s.ItemInstance.Item.ItemType == ItemType.House && s.ItemInstance.Item.ItemSubType
                                == minilandobject.Item.ItemSubType);
                            if (min != null)
                                new RmvobjPacketHandler(Session).MinilandRemoveObject(new RmvobjPacket
                                { Slot = min.ItemInstance.Slot });
                        }

                        Session.Character.MinilandObjects.Add(minilandobj);
                        Session.SendPacket(minilandobj.GenerateMinilandEffect(false));
                        Session.SendPacket(Session.Character.GenerateMinilandPoint());
                        Session.SendPacket(minilandobj.GenerateMinilandObject(false));
                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MINILAND_NEED_LOCK"),
                                0));
                    }
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(
                            Language.Instance.GetMessageFromKey("ALREADY_THIS_MINILANDOBJECT"), 0));
                }
            }
        }

        #endregion
    }
}