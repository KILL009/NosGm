using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.PathFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SpecialActionsHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SpecialActions;

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
            var card = evnt.Card;

            if (SubType.Equals((byte)AdditionalTypes.SpecialActions.PushBack))
            {
                if (target.ResistForcedMovement <= 0
                    || ServerManager.RandomNumber() < target.ResistForcedMovement)
                {
                    target.PushBackSession(firstData, target, caster);
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.SpecialActions.Hide))
            {
                if (FirstData >= 0)
                {
                    if (target.Character is Character charact)
                    {
                        if (charact.MapInstance.MapInstanceType != MapInstanceType.NormalInstance || charact.MapInstance.Map.MapId != 2004)
                        {
                            charact.Invisible = true;
                            charact.Mates.Where(s => s.IsTeamMember).ToList().ForEach(s => charact.Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                            charact.Session.CurrentMapInstance?.Broadcast(charact.GenerateInvisible());
                        }
                        else if (card != null)
                        {
                            charact.RemoveBuff(card.CardId);
                        }
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.SpecialActions.FocusEnemies))
            {
                if (target.ResistForcedMovement <= 0
                    || ServerManager.RandomNumber() < target.ResistForcedMovement)
                {
                    if ((target.MapMonster == null || !target.MapMonster.IsBoss && !ServerManager.Instance.BossVNums.Contains(target.MapMonster.MonsterVNum) || target.MapMonster.Owner?.Character != null || target.MapMonster.Owner?.Mate != null)
                        && (target.MapNpc == null || !ServerManager.Instance.BossVNums.Contains(target.MapNpc.NpcVNum)))
                    {
                        Observable.Timer(TimeSpan.FromMilliseconds(skill.CastTime * 100)).Subscribe(s =>
                        {
                            if (!target.MapInstance.Map.isBlockedZone(target.PositionX, target.PositionY, caster.PositionX, caster.PositionY))
                            {
                                target.PositionX = caster.PositionX;
                                target.PositionY = caster.PositionY;
                                target.MapInstance.Broadcast($"guri 3 {(short)target.UserType} {target.MapEntityId} {target.PositionX} {target.PositionY} 3 {SecondData} 2 -1");
                            }
                        });
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.SpecialActions.RunAway))
            {
                if (target.MapMonster != null && target.MapMonster.IsMoving && !target.MapMonster.IsBoss && !ServerManager.Instance.BossVNums.Contains(target.MapMonster.MonsterVNum) && target.MapMonster.Owner?.Character == null && target.MapMonster.Owner?.Mate == null)
                {
                    if (target.MapMonster.Target != null)
                    {
                        (target.MapMonster.Path ?? (target.MapMonster.Path = new List<GridPos>())).Clear();
                        target.MapMonster.Target = null;

                        short RunToX = target.MapMonster.FirstX;
                        short RunToY = target.MapMonster.FirstY;

                        MapCell RunToPos = target.MapInstance.Map.GetRandomPositionByDistance(target.PositionX, target.PositionY, 20);
                        if (RunToPos != null)
                        {
                            RunToX = RunToPos.X;
                            RunToY = RunToPos.Y;
                        }

                        /*session.MapMonster.Path = BestFirstSearch.FindPathJagged(new Node { X = session.MapMonster.MapX, Y = session.MapMonster.MapY }, new Node { X = RunToX, Y = RunToY },
                            session.MapMonster.MapInstance.Map.JaggedGrid);*/
                        target.MapMonster.RunToX = RunToX;
                        target.MapMonster.RunToY = RunToY;
                        Observable.Timer(TimeSpan.FromMilliseconds(card != null ? card.Duration * 100 : 10000)).Subscribe(s => { target.MapMonster.RunToX = 0; target.MapMonster.RunToY = 0; });
                    }
                }
            }
        }
    }
}