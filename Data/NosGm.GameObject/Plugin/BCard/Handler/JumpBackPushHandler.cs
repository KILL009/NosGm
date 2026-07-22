using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using System;
using System.Windows.Media.Media3D;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class JumpBackPushHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.JumpBackPush;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var skill = evnt.Skill;
            var ThirdData = evnt.BCard.ThirdData;
            var CardId = evnt.BCard.CardId;
            var SkillVNum = evnt.BCard.SkillVNum;
            var BCardId = evnt.BCard.BCardId;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;
            var delaytime = evnt.DelayTime;
            var duration = evnt.Duration;

            if (target.ResistForcedMovement <= 0
                            || ServerManager.RandomNumber() < target.ResistForcedMovement)
            {
                if (SubType.Equals((byte)AdditionalTypes.JumpBackPush.JumpBackChance))
                {
                    if (ServerManager.RandomNumber() < firstData)
                    {
                        caster.PushBackSession(SecondData, caster, target);
                    }
                }
                if (SubType.Equals((byte)AdditionalTypes.JumpBackPush.PushBackChance))
                {
                    if (ServerManager.RandomNumber() < firstData)
                    {
                        target.PushBackSession(SecondData, target, caster);
                    }
                }
            }
        }
    }
}
