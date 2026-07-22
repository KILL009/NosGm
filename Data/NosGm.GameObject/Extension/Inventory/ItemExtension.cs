using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using NosGm.XMLModel.Events;

using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosGm.GameObject.Extension.Message;

namespace NosGm.GameObject.Extension
{
    public static class ItemExtension
    {
        #region Methods


        public static void ClearShell(this ItemInstance i)
        {
            i.ShellEffects.Clear();
            i.RuneEffects.Clear();
        }

        public static void AddSkill(ClientSession session, short SkillToAdd, short HeroLevelNeeded)
        {
            if (session.Character.HeroLevel >= HeroLevelNeeded)
            {
                session.Character.AddSkill(SkillToAdd);
            }
            else
            {
                MessageExtension.SendBubble(session, "Your Hero Level is not high enough");
            }
        }

        #endregion
    }
}