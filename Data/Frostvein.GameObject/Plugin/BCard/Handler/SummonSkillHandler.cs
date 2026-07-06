using Game.Configuration.BCards;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SummonSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SummonSkill;

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

            if (SubType.Equals((byte)AdditionalTypes.SummonSkill.TransformToBear))
            {
                Character user = caster.Character != null ? caster.Character : target.Character;

                if (user == null)
                {
                    return;
                }

                if (user.Morph == 43) return;

                Card morphCard = ServerManager.GetCard(CardId.Value);

                if (morphCard == null)
                {
                    return;
                }
                user.Morph = 43;
                user.Session.SendPacket(user.GenerateCMode());
                user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                user.DragonModeObservable?.Dispose();
                user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));

                user.DragonModeObservable = Observable.Timer(TimeSpan.FromSeconds(morphCard.Duration * 0.1)).Subscribe(s =>
                {
                    user.Morph = 42;
                    user.Session.SendPacket(user.GenerateCMode());
                    user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                    user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));
                });
            }

            if (SubType.Equals((byte)AdditionalTypes.SummonSkill.TransformToDruid))
            {
                Character user = caster.Character != null ? caster.Character : target.Character;

                if (user == null)
                {
                    return;
                }

                if (user.Morph == 42) return;

                user.DragonModeObservable?.Dispose();

                if (user.Morph != 43)
                {
                    foreach (Mate teamMate in user.Mates?.Where(m => m != null && m.IsTeamMember))
                    {
                        teamMate.PositionX = user.PositionX;
                        teamMate.PositionY = user.PositionY;
                        teamMate.UpdateBushFire();

                        if (user.MapInstance?.Sessions != null)
                        {
                            Parallel.ForEach(user.MapInstance.Sessions.Where(s => s?.Character != null), s =>
                            {
                                if (ServerManager.Instance.ChannelId != 51 || user.Faction == s.Character.Faction)
                                {
                                    s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                }
                                else
                                {
                                    s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                }
                            });
                        }

                        user.Session?.SendPacket(user.GeneratePinit());
                        user.Mates?.ForEach(s => user.Session?.SendPacket(s.GenerateScPacket()));
                        user.Session?.SendPackets(user.GeneratePst());
                    }

                }
                user.IsVehicled = false;
                user.VehicleSpeed = 0;
                user.LoadSpeed();
                user.MorphUpgrade = 0;
                user.MorphUpgrade2 = 0;
                user.Morph = 42;
                user.Session.SendPacket(user.GenerateCMode());
                user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));
                return;

            }

            if (SubType.Equals((byte)AdditionalTypes.SummonSkill.Summon12) || 
                SubType.Equals((byte)AdditionalTypes.SummonSkill.Summon10))
            {
                int amount = 0;
                int aliveTime = 0;
                if (SubType.Equals((byte)AdditionalTypes.SummonSkill.Summon12))
                {
                    amount = 1;
                }

                if (SubType.Equals((byte)AdditionalTypes.SummonSkill.Summon10))
                {
                    amount = 1;
                }

                if (ServerManager.RandomNumber() < firstData)
                {
                    aliveTime = ServerManager.GetNpcMonster((short)SecondData).RespawnTime / (ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 2400 ? ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 150 ? 1 : 10 : 40) * (ServerManager.GetNpcMonster((short)SecondData).RespawnTime >= 150 ? 4 : 1);
                    bool canMove = SecondData != 797 && SecondData != 798 && SecondData != 2013 && SecondData != 2016;
                    List<MonsterToSummon> monstersToSummon = new List<MonsterToSummon>();
                    for (int i = 0; i < (amount > 0 ? amount : 1); i++)
                    {
                        x = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionX);
                        y = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionY);

                        if (skill != null && caster.Character == null)
                        {
                            MapCell randomCell = caster.MapInstance.Map.GetRandomPositionByDistance(caster.PositionX, caster.PositionY, skill.Range, true);

                            if (randomCell != null)
                            {
                                x = randomCell.X;
                                y = randomCell.Y;
                            }
                        }

                        monstersToSummon.Add(new MonsterToSummon((short)SecondData, new MapCell { X = x, Y = y }, null, canMove, aliveTimeMp: aliveTime, owner: caster));
                    }
                    EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, monstersToSummon));
                }
            }

        }
    }
}
