using Frostvein.Core;
using Frostvein.Domain;
using System;

namespace Frostvein.GameObject.Helpers
{
    public static class ChangeClassHelper
    {
        #region Methods

        public static void ChangeClass(this ClientSession session, ItemInstance inv)
        {
            if (session.Character.HasShopOpened || session.Character.InExchangeOrTrade)
            {
                session.Character.DisposeMap();
            }

            if (session.Character.IsChangingMapInstance || session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4Instance)
            {
                return;
            }

            if (DateTime.Now < session.Character.LastSkillUse.AddSeconds(20) || DateTime.Now < session.Character.LastDefence.AddSeconds(20))
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 10));
                return;
            }

            if (session.Character.Inventory.Any(i => i.Type == InventoryType.Wear))
            {
                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                return;
            }

            switch (inv.Item.EffectValue)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    if (session.Character.IsVehicled && session.Character.Class == ClassType.MartialArtist)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 10));
                        return;
                    }

                    if (session.Character.Level < 99)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REQUIRED_LEVEL_CHANGER"), 0));
                        return;
                    }

                    session.Character.ChangeClass((ClassType)inv.Item.EffectValue, true);
                    break;

                case 4:
                    if (session.Character.Level < 99)
                    {
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REQUIRED_LEVEL_CHANGER"), 0));
                        return;
                    }

                    session.Character.ChangeClass((ClassType)inv.Item.EffectValue, true);
                    break;

                default:
                    return;
            }

            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
        }

        #endregion
    }
}