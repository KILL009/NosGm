using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using Frostvein.XMLModel.Events;

using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Frostvein.GameObject.Extension.Message;

namespace Frostvein.GameObject.Extension
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