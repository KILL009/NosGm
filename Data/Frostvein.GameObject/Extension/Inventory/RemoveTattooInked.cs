using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class RemoveTattooInked
    {
        #region Methods

        public static void RemoveTattoo(this CharacterSkill e, ClientSession s)
        {

            if (s.Character.Inventory.CountItem(5799) < 1)
            {
                // Not Enough Item
                s.SendShopEnd();
                return;
            }

            if (!e.IsTattoo)
            {
                s.SendShopEnd();
                return;
            }

            var skill = ServerManager.GetSkill(e.SkillVNum);

            if (skill.Class != 27)
            {
                s.SendShopEnd();
                return;
            }

            var msg = $"The {skill.Name} tattoo has been removed";
            s.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            s.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            s.Character.Inventory.RemoveItemAmount(5799);
            s.Character.Skills.Remove(e.SkillVNum);
            s.SendPacket(s.Character.GenerateSki());
            s.SendPackets(s.Character.GenerateQuicklist());
            s.SendPacket(s.Character.GenerateLev());
            s.SendShopEnd();
        }

        #endregion
    }
}