using Game.Configuration.BCards;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class HugeSnowmanHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.HugeSnowman;

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

            if (SubType == (byte)AdditionalTypes.HugeSnowman.SnowStorm)
            {
                if (caster.CanAttackEntity(target))
                {
                    target.GetDamage((int)(target.HpMax * 0.5D), caster, false, true);
                    target.Character?.Session.SendPacket(target.Character.GenerateStat());
                    target.Mate?.Owner.Session.SendPacket(target.Mate.GenerateStatInfo());
                    if (target.Hp <= 0)
                    {
                        target.MapInstance.Broadcast(StaticPacketHelper.Die(target.UserType, target.MapEntityId, target.UserType, target.MapEntityId));
                        if (target.Character != null)
                        {
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(obs =>
                            {
                                ServerManager.Instance.AskRevive(target.Character.CharacterId);
                            });
                        }
                    }
                }
            }
        }
    }
}
