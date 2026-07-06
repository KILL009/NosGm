using Frostvein.Domain;
using Frostvein.GameObject.Plugin.Load.Handler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.GameObject.ItemThread
{
    public static class WingsThread
    {
        public static void ApplyBuff(ClientSession session, WingBuffType wingBuffType)
        {
            session.Character.AddBuff(new Buff((short)wingBuffType, session.Character.Level, true), session.Character.BattleEntity, true);
        }

        public static void RemoveBuff(ClientSession session)
        {
            session.Character.RemoveBuff(387, true); //Titan Wings
            session.Character.RemoveBuff(395, true); //Archangel Wings
            session.Character.RemoveBuff(396, true); //Archdaemon Wings
            session.Character.RemoveBuff(397, true); //Blazing Fire Wings
            session.Character.RemoveBuff(398, true); //Frosty Ice Wings
            session.Character.RemoveBuff(410, true); //Golden Wings
            session.Character.RemoveBuff(411, true); //Onyx Wings
            session.Character.RemoveBuff(444, true); //Fairy Wings
            session.Character.RemoveBuff(663, true); //Zephyr Wings
            session.Character.RemoveBuff(686, true); //Lightning Wings
            session.Character.RemoveBuff(755, true); //Mega Titan Wings
            session.Character.RemoveBuff(838, true); //Blade Wings
            session.Character.RemoveBuff(851, true); //Crystal Wings
            session.Character.RemoveBuff(926, true); //Petal Wings
            session.Character.RemoveBuff(985, true); //Lunar Wings
            session.Character.RemoveBuff(1444, true); //Retro Wings
            session.Character.RemoveBuff(4002, true); //Golden Eagle Wings
        }
    }
}
