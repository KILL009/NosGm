using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Collections.Generic;

namespace Frostvein.GameObject.Extension.Inventory
{

    public static class UpgradeTattooExtension
    {
        #region Method

        public static void UpgradeTattoo(this CharacterSkill e, ClientSession s, bool isProtected)
        {
            #region Configuration
            byte[] percentDestroyed = { 0, 10, 15, 25, 30, 35, 50, 70, 90 };
            int[] goldPrice = { 30000, 67000, 140000, 230000, 380000, 540000, 770000, 960000, 1200000 };
            short[] percentSuccesss = { 80, 60, 50, 35, 20, 10, 5, 2, 1 };
            short[] percentFail = { 20, 30, 35, 40, 50, 55, 45, 28, 9 };

            #endregion

            if (isProtected && s.Character.Inventory.CountItem(5815) < 1)
            {
                s.SendShopEnd();
                return;
            }

            if (e.TattooLevel == 9)
            {
                s.SendShopEnd();
                return;
            }

            if (!e.IsTattoo)
            {
                s.SendShopEnd();
                return;
            }

            var skill = ServerManager.GetSkill(e.SkillVNum);
            var value = e.TattooLevel;

            if (skill.Class != 27)
            {
                s.SendShopEnd();
                return;
            }


            if (s.Character.Gold < goldPrice[value])
            {
                s.SendShopEnd();
                return;
            }

            //Count Items from Inventory, add NotEnoughItem, SendShopEnd

            var rnd = ServerManager.RandomNumber();
            string msg;
            int effectId;
            if (rnd < percentDestroyed[value]) // fail + level --
            {
                if (!isProtected)
                {
                    e.TattooLevel--;
                    effectId = 3003;
                    msg = $"The {skill.Name} tattoo improvement FAILED ! and Decreased ! -{e.TattooLevel}";
                }
                else
                {
                    effectId = 3004;
                    msg = $"The {skill.Name} tattoo improvement FAILED ! But the level was saved with the scroll !";
                }
            }
            else if (rnd < percentFail[value]) // fail
            {
                effectId = 3004;
                msg = $"The {skill.Name} tattoo improvement FAILED !";
            }
            else // success
            {
                e.TattooLevel++;
                effectId = 3005;
                msg = $"The {skill.Name} tattoo has been improved ! +{e.TattooLevel}";
            }


            if (isProtected) s.Character.Inventory.RemoveItemAmount(5815);

            s.GoldLess(goldPrice[value]);
            s.SendPacket(s.Character.GenerateSki());
            s.SendPackets(s.Character.GenerateQuicklist());
            s.CurrentMapInstance.Broadcast(
                StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, effectId),
                s.Character.PositionX, s.Character.PositionY);
            s.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            s.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            s.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, s.Character.CharacterId, 2388));
            s.SendShopEnd();
        }

        #endregion
    }
}