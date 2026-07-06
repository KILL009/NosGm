using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using System.Linq;

namespace Frostvein.GameObject
{
    public class TitleItem : Item
    {
        #region Instantiation

        public TitleItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (session.Character.IsVehicled)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance) return;

            if (session.Character.Inventory.CountItem(VNum) < 1) return;

            if (session.Character.Title.Any(s => s.TitleVnum == VNum))
            {
                session.SendPacket(UserInterfaceHelper.GenerateInfo("You already have this Title!"));
                return;
            }
            session.SendPacket($"qna #guri^306^{VNum}^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_TITLE")}");
        }

        #endregion
    }
}