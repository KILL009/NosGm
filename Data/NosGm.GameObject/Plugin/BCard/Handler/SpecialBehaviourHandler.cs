using Game.Configuration.BCards;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SpecialBehaviourHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SpecialBehaviour;

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

            if (SubType == (byte)AdditionalTypes.SpecialBehaviour.TeleportRandom)
            {
                if (caster.Character != null)
                {
                    MapCell randomCell = target.MapInstance.Map.GetRandomPosition();
                    target.PositionX = randomCell.X;
                    target.PositionY = randomCell.Y;
                    target.MapInstance.Broadcast(target.GenerateTp());
                }
                else
                {
                    MapCell randomCell = null;

                    if (SecondData > 0)
                    {
                        randomCell = caster.MapInstance.Map.GetRandomPositionByDistance(caster.PositionX, caster.PositionY, (short)SecondData, true);
                    }
                    else
                    {
                        randomCell = caster.MapInstance.Map.GetRandomPosition();
                    }

                    caster.PositionX = randomCell.X;
                    caster.PositionY = randomCell.Y;
                    caster.MapInstance.Broadcast(caster.GenerateTp());
                }
            }
            else if (SubType == (byte)AdditionalTypes.SpecialBehaviour.InflictOnTeam)
            {
                if (CardId != null && CardId.Value == SecondData) // Checked on official server
                {
                    return;
                }
                void InflictOnTeamAction()
                {
                    if (target.Hp < 1
                        || target.MapInstance == null)
                    {
                        return;
                    }

                    target.MapInstance.GetBattleEntitiesInRange(new MapCell { X = target.PositionX, Y = target.PositionY }, (byte)FirstData)
                        .Where(s => s.EntityType != EntityType.Npc && (s.EntityType != target.EntityType || s.MapEntityId != target.MapEntityId) && !target.CanAttackEntity(s)).ToList()
                        .ForEach(s => s.AddBuff(new Buff((short)SecondData, casterLevel), caster));
                }

                if (ThirdData > 0)
                {
                    IDisposable bcardDisposable = null;
                    bcardDisposable = Observable.Interval(TimeSpan.FromSeconds(ThirdData * 2))
                        .Subscribe(s =>
                        {
                            if (target.BCardDisposables[BCardId] != bcardDisposable)
                            {
                                bcardDisposable.Dispose();
                                return;
                            }
                            InflictOnTeamAction();
                        });
                    target.BCardDisposables[BCardId] = bcardDisposable;
                }
            }
        }
    }
}
