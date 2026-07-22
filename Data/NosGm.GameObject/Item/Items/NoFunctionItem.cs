

using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Collections.Generic;


namespace NosGm.GameObject
{
    public class NoFunctionItem : Item
    {
        #region Instantiation

        public NoFunctionItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            if (session.Character.IsVehicled)
            {
                session.SendPacket(
                    session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance) return;

            switch (inv.ItemVNum)
            {
                // Unlock hero levels from 60 to 70
                //case 1158:
                //    if (session.Character.UnlockedHLevel.CheckIfInRange(60, 69))
                //    {
                //        session.Character.UnlockedHLevel = 70;
                //        session.SendPacket(UserInterfaceHelper.GenerateMsg($"You have unlocked Hero Level {session.Character.UnlockedHLevel}", 0));
                //        session.Character.Inventory.RemoveItemAmount(inv.ItemVNum);
                //    }

                //    return;

                case 1143:
                    ItemInstance sp = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (session.Character.UseSp && sp != null && !session.Character.IsSeal)
                    {
                        List<long> spVNum = new List<long> { 902, 903, 913 };
                        if (!spVNum.Contains(sp.Item.VNum))
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_USE_THAT_SP"), 0));
                            return;
                        }
                        if (Option == 0)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_SKIN_CHANGE")}");
                        }
                        else
                        {
                            sp.HasSkin = true;
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
                            session.SendPacket(session.Character.GenerateStat());
                            session.SendPackets(session.Character.GenerateStatChar());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"), 0));
                    }
                    break;

                default:
                    break;
            }

            switch (Effect)
            {
                case 10:
                    {
                        switch (EffectValue)
                        {
                            case 1:
                                if (session.Character.Inventory.CountItem(1036) < 1 ||
                                    session.Character.Inventory.CountItem(1013) < 1) return;
                                session.Character.Inventory.RemoveItemAmount(1036);
                                session.Character.Inventory.RemoveItemAmount(1013);
                                if (ServerManager.RandomNumber() < 25)
                                    switch (ServerManager.RandomNumber(0, 2))
                                    {
                                        case 0:
                                            session.Character.GiftAdd(1015, 3);
                                            break;

                                        case 1:
                                            session.Character.GiftAdd(1016, 2);
                                            break;
                                    }

                                break;

                            case 2:
                                if (session.Character.Inventory.CountItem(1038) < 1 ||
                                    session.Character.Inventory.CountItem(1013) < 1) return;
                                session.Character.Inventory.RemoveItemAmount(1038);
                                session.Character.Inventory.RemoveItemAmount(1013);
                                if (ServerManager.RandomNumber() < 25)
                                    switch (ServerManager.RandomNumber(0, 4))
                                    {
                                        case 0:
                                            session.Character.GiftAdd(1031, 3);
                                            break;

                                        case 1:
                                            session.Character.GiftAdd(1032, 3);
                                            break;

                                        case 2:
                                            session.Character.GiftAdd(1033, 3);
                                            break;

                                        case 3:
                                            session.Character.GiftAdd(1034, 3);
                                            break;
                                    }

                                break;

                            case 3:
                                if (session.Character.Inventory.CountItem(1037) < 1 ||
                                    session.Character.Inventory.CountItem(1013) < 1) return;
                                session.Character.Inventory.RemoveItemAmount(1037);
                                session.Character.Inventory.RemoveItemAmount(1013);
                                if (ServerManager.RandomNumber() < 25)
                                    switch (ServerManager.RandomNumber(0, 17))
                                    {
                                        //case 0:
                                        //case 1:
                                        //case 2:
                                        //case 3:
                                        //case 4:
                                        //    session.Character.GiftAdd(1017, 5); /cellon lv1
                                        //    break;

                                        //case 5:
                                        //case 6:
                                        //case 7:
                                        //case 8:
                                        //    session.Character.GiftAdd(1018, 4); /cellon lv2
                                        //    break;

                                        case 9:
                                        case 10:
                                        case 11:
                                            session.Character.GiftAdd(1019, 4);
                                            break;

                                        case 12:
                                        case 13:
                                            session.Character.GiftAdd(1020, 3);
                                            break;

                                        case 14:
                                            session.Character.GiftAdd(1021, 3);
                                            break;

                                        case 15:
                                            session.Character.GiftAdd(1022, 2);
                                            break;

                                        case 16:
                                            session.Character.GiftAdd(1023, 2);
                                            break;
                                    }

                                break;
                        }

                        session.Character.GiftAdd(1014, (byte)ServerManager.RandomNumber(5, 11));
                    }
                    break;

                default:
                    //LOGGER($"[HANDLER] Handler not found for: {GetType()} | {VNum} | {Effect} | {EffectValue}");
                    break;
            }


        }

        #endregion
    }
}