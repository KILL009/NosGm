using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Frostvein.Domain.BCardType;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Plugin.Event;

namespace Frostvein.Extension.Extension.Packet
{
    public static class BattleExt
    {
        #region Methods

        public static void PvpHit(this ClientSession Session, HitRequest hitRequest, ClientSession target)
        {
            if (target?.Character.Hp > 0 && hitRequest?.Session.Character.Hp > 0)
            {
                if (target.Character.IsSitting)
                {
                    target.Character.Rest();
                }

                double cooldownReduction = Session.Character.GetBuff(BCardType.CardType.Morale,
                                                   (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0] +
                                           Session.Character.GetBuff(BCardType.CardType.Casting,
                                                   (byte)AdditionalTypes.Casting.EffectDurationIncreased)[0];
                var mCd = new BattleEntity(hitRequest.Session.Character, hitRequest.Skill);
                if (mCd.AttackType == AttackType.Magical)
                {
                    cooldownReduction += Session.Character.GetBuff(BCardType.CardType.AbsorbedSpirit,
                                                   (byte)AdditionalTypes.AbsorbedSpirit.MagicCooldownDecreased)[0];
                }

                var increaseEnemyCooldownChance = Session.Character.GetBuff(BCardType.CardType.DarkCloneSummon,
                    (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                {
                    cooldownReduction -= increaseEnemyCooldownChance[1];
                }

                var hitmode = 0;
                var onyxWings = false;
                var zephyrWings = false;
                var dragonBuff = false;
                var battleEntity = new BattleEntity(hitRequest.Session.Character, hitRequest.Skill);
                var battleEntityDefense = new BattleEntity(target.Character, null);
                var damage = DamageHelper.Instance.CalculateDamage(battleEntity, battleEntityDefense, hitRequest.Skill,
                    ref hitmode, ref onyxWings, ref zephyrWings, ref dragonBuff);

                if (target.Character.HasGodMode)
                {
                    damage = 0;
                    hitmode = 4;
                }
                else if (target.Character.LastPVPRevive > DateTime.Now.AddSeconds(-10)
                         || hitRequest.Session.Character.LastPVPRevive > DateTime.Now.AddSeconds(-10))
                {
                    damage = 0;
                    hitmode = 4;
                }

                if (ServerManager.RandomNumber() < target.Character.GetBuff(BCardType.CardType.DarkCloneSummon,
                        (byte)AdditionalTypes.DarkCloneSummon.ConvertDamageToHPChance)[0])
                {
                    var amount = damage / 2;

                    target.Character.ConvertedDamageToHP += amount;
                    target.Character.MapInstance?.Broadcast(target.Character.GenerateRc(amount));
                    target.Character.Hp += amount;

                    if (target.Character.Hp > target.Character.HPLoad())
                    {
                        target.Character.Hp = (int)target.Character.HPLoad();
                    }

                    target.SendPacket(target.Character.GenerateStat());

                    damage = 0;
                }

                if (ServerManager.RandomNumber() < target.Character.GetBuff(BCardType.CardType.GuarantedDodgeRangedAttack,
                        (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AlwaysDodgePropability)[0])
                {
                    damage = 0;
                    hitmode = 4;
                }

                if (hitmode != 4 && hitmode != 2)
                {
                    Session.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                    {
                        new KeyValuePair<byte, byte>((byte) BCardType.CardType.SpecialActions,
                            (byte) AdditionalTypes.SpecialActions.Hide)
                    });
                    target.Character.RemoveBuffByBCardTypeSubType(new List<KeyValuePair<byte, byte>>
                    {
                        new KeyValuePair<byte, byte>((byte) BCardType.CardType.SpecialActions,
                            (byte) AdditionalTypes.SpecialActions.Hide)
                    });
                    target.Character.RemoveBuff(36);
                    target.Character.RemoveBuff(548);
                }

                if (Session.Character.Buff.FirstOrDefault(s => s.Card.BCards.Any(b =>
                    b.Type == (byte)BCardType.CardType.FalconSkill &&
                    b.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide))) is Buff FalconHideBuff)
                {
                    Session.Character.RemoveBuff(FalconHideBuff.Card.CardId);
                    Session.Character.AddBuff(new Buff(560, Session.Character.Level), Session.Character.BattleEntity);
                }

                var manaShield = target.Character.GetBuff(BCardType.CardType.LightAndShadow,
                    (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP);
                if (manaShield[0] != 0 && hitmode != 4)
                {
                    var reduce = damage / 100 * manaShield[0];
                    if (target.Character.Mp < reduce)
                    {
                        reduce = target.Character.Mp;
                        target.Character.Mp = 0;
                    }
                    else
                    {
                        target.Character.DecreaseMp(reduce);
                    }                    
                }

                if (onyxWings && hitmode != 4 && hitmode != 2)
                {
                    var onyxX = (short)(hitRequest.Session.Character.PositionX + 2);
                    var onyxY = (short)(hitRequest.Session.Character.PositionY + 2);
                    var onyxId = target.CurrentMapInstance.GetNextMonsterId();
                    var onyx = new MapMonster
                    {
                        MonsterVNum = 2371,
                        MapX = onyxX,
                        MapY = onyxY,
                        MapMonsterId = onyxId,
                        IsHostile = false,
                        IsMoving = false,
                        ShouldRespawn = false
                    };
                    target.CurrentMapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                        hitRequest.Session.Character.CharacterId, onyxX, onyxY));
                    onyx.Initialize(target.CurrentMapInstance);
                    target.CurrentMapInstance.AddMonster(onyx);
                    target.CurrentMapInstance.Broadcast(onyx.GenerateIn());
                    target.Character.GetDamage((int)(damage / 2D), battleEntity);
                    Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                    {
                        target?.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, 1, target.Character.CharacterId, -1, 0, -1, hitRequest.Skill.Effect, -1, -1, true, 92,
                            (int)(damage / 2D), 0, 0));
                        target?.CurrentMapInstance?.RemoveMonster(onyx);
                        target?.CurrentMapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                            onyx.MapMonsterId));
                    });
                }

                if (zephyrWings && hitmode != 1)
                {
                    target.Character.GetDamage(damage / 4, battleEntity);
                    var damage1 = damage;
                    target?.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                        hitRequest.Session.Character.CharacterId, 1,
                        target.Character.CharacterId, -1, 0, -1, 4211, -1, -1, true, 92, damage1 / 4, 0, 1));
                }

                if (dragonBuff && hitmode != 1) // Dragon hit in pvp
                {
                    target.Character.GetDamage(damage / 1, battleEntity);
                    var damage1 = damage;
                    target.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, hitRequest.Session.Character.CharacterId, 1,
                        target.Character.CharacterId, -1, 0, -1, 7922, -1, -1, true, 92, damage1 * 70 / 100, 0, 1));
                }

                //Add Golden Eagle using 7915

                target.Character.GetDamage(damage / 2, battleEntity);
                target.SendPacket(target.Character.GenerateStat());


                // Magical Fetters
                if (damage > 0)
                {
                    if (target.Character.HasMagicalFetters)
                    {
                        // Magic Spell
                        //TODO: Rework this
                        target.Character.AddBuff(new Buff(617, target.Character.Level), target.Character.BattleEntity);
                        switch (Session.Character.Element)
                        {
                            case 0:
                                target.Character.LastComboCastId = 1195;
                                target.SendPacket($"mslot 1195 -1");
                                break;

                            case 1:
                                target.Character.LastComboCastId = 1191;
                                target.SendPacket($"mslot 1191 -1");
                                break;

                            case 2:
                                target.Character.LastComboCastId = 1192;
                                target.SendPacket($"mslot 1192 -1");
                                break;

                            case 3:
                                target.Character.LastComboCastId = 1193;
                                target.SendPacket($"mslot 1193 -1");
                                break;

                            case 4:
                                target.Character.LastComboCastId = 1194;
                                target.SendPacket($"mslot 1194 -1");
                                break;
                        }
                    }
                }

                var isAlive = target.Character.Hp > 0;

                if (target.Character.DamageList.ContainsKey(hitRequest.Session.Character.CharacterId))
                {
                    target.Character.DamageList[hitRequest.Session.Character.CharacterId] += damage;
                }
                else
                {
                    target.Character.DamageList.Add(hitRequest.Session.Character.CharacterId, damage);
                }

                if (!isAlive && target.HasCurrentMapInstance)
                {
                    if (target.Character.IsVehicled)
                    {
                        target.Character.RemoveVehicle();
                    }

                    if (hitRequest.Session.Character != null && hitRequest.SkillBCards.FirstOrDefault(s =>
                                                                                   s.Type == (byte)BCardType.CardType.TauntSkill &&
                                                                                   s.SubType == (byte)AdditionalTypes.TauntSkill.EffectOnKill) is BCard EffectOnKill)
                    {
                        if (ServerManager.RandomNumber() < EffectOnKill.FirstData)
                        {
                            hitRequest.Session.Character.AddBuff(new Buff((short)EffectOnKill.SecondData, hitRequest.Session.Character.Level), hitRequest.Session.Character.BattleEntity);
                        }
                    }


                    target.Character.LastPvPKiller = Session;
                    if (target.CurrentMapInstance.Map?.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4) == true)
                    {
                        if (ServerManager.Instance.ChannelId == 51 && ServerManager.Instance.Act4DemonStat.Mode == 0 && ServerManager.Instance.Act4AngelStat.Mode == 0)
                        {
                            switch (Session.Character.Faction)
                            {
                                case FactionType.Angel:
                                    ServerManager.Instance.Act4AngelStat.Percentage += 10000 / (GlacernonConfigurationExtension.GlacernonRatePVP * 20);

                                    break;

                                case FactionType.Demon:
                                    ServerManager.Instance.Act4DemonStat.Percentage += 10000 / (GlacernonConfigurationExtension.GlacernonRatePVP * 20);

                                    break;
                            }
                        }

                        hitRequest.Session.Character.Act4Kill++;
                        hitRequest.Session.Character.IncrementQuests(QuestType.GlacernonQuest, 1);

                        target.Character.Act4Dead++;
                        target.Character.GetAct4Points(-1);
                        if (target.Character.Level + 15 >= hitRequest.Session.Character.Level && hitRequest.Session.Character.Level <= target.Character.Level - 15)
                        {
                            hitRequest.Session.Character.GetAct4Points(2);
                        }

                        var repRemoved = 0;
                        var ReputationValue = 0;

                        if (target.CleanIpAddress != hitRequest.Session.CleanIpAddress)
                        {
                            target.Character.GetReputation(-3000);
                            target.GoldLess(50000);

                            var alreadyHaveRep = new List<long>();
                            var amount = target.Character.DamageList.Keys.Count();
                            foreach (var charId in target.Character.DamageList.Keys)
                            {
                                var session = ServerManager.Instance.GetSessionByCharacterId(charId);

                                if (session == null)
                                {
                                    continue;
                                }

                                if (!session.CurrentMapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                                {
                                    continue;
                                }

                                var levelDifference = target.Character.Level - session.Character.Level;

                                if (levelDifference >= 40)
                                {
                                    hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TOO_LEVEL_DIFFERENCE"), 11));
                                    continue;
                                }

                                if (levelDifference >= 0)
                                {
                                    ReputationValue = 3000 + levelDifference * 10;
                                }
                                else if (levelDifference > -40)
                                {
                                    ReputationValue = 1500 - levelDifference * 10;
                                }
                                else
                                {
                                    ReputationValue -= 500 + -levelDifference * 10;
                                }

                                ReputationValue *= GameConfiguration.ReputationRate;
                                repRemoved = repRemoved == 0 ? ReputationValue : repRemoved;

                                if (target.Character.ReputationHeroPosition() != 0 && target.Character.ReputationHeroPosition() <= 3) //test it
                                {
                                    ReputationValue *= 3;
                                }

                                if (target.Character.ReputationHeroPosition() > 3)
                                {
                                    ReputationValue *= 2;
                                }

                                if (ReputationValue < 0)
                                {
                                    continue;
                                }

                                if (alreadyHaveRep.Contains(charId))
                                {
                                    continue;
                                }

                                session.Character.Reputation += ReputationValue / amount;
                                session.SendPacket(hitRequest.Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), (short)ReputationValue), 12));
                                session.SendPacket(session.Character.GenerateFd());
                            }

                            var Act4RaidPenalty = target.Character.Faction == FactionType.Angel && ServerManager.Instance.Act4DemonStat.Mode == 3 || target.Character.Faction == FactionType.Demon && ServerManager.Instance.Act4AngelStat.Mode == 3 ? 5 : 5;
                            target.Character.DamageList = new Dictionary<long, long>();
                            target.Character.Reputation -= repRemoved /* * Act4RaidPenalty*/;
                            target.SendPacket(target.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LOSE_REP"), (short)repRemoved /* * Act4RaidPenalty*/), 11));
                            target.SendPacket(target.Character.GenerateFd());
                            hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateLev());
                        }
                        else
                        {
                            //penalties for the pvpkiller
                            hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TARGET_SAME_IP"), 11));
                            hitRequest.Session.Character.GetAct4Points(-1);
                            hitRequest.Session.Character.Act4Kill--;
                            hitRequest.Session.Character.Reputation -= 10000;

                            if (hitRequest.Session.Character.Reputation < 1) // prevent rep points going negative
                            {
                                hitRequest.Session.Character.Reputation = 1;
                            }

                            // revert the stats as they were before after the kill
                            target.Character.Act4Dead--;
                            target.Character.GetAct4Points(1);

                        }

                        foreach (var sess in ServerManager.Instance.Sessions.Where(s => s.HasSelectedCharacter))
                        {
                            if (sess.Character.Faction == Session.Character.Faction)
                            {
                                sess.SendPacket(sess.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey($"ACT4_PVP_KILL{(int)target.Character.Faction}"), Session.Character.Name), 12));

                            }
                            else if (sess.Character.Faction == target.Character.Faction)
                            {
                                sess.SendPacket(sess.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey($"ACT4_PVP_DEATH{(int)target.Character.Faction}"), target.Character.Name), 11));

                            }
                        }

                        target.SendPacket(target.Character.GenerateFd());
                        target.CurrentMapInstance?.Broadcast(target, target.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                        target.CurrentMapInstance?.Broadcast(target, target.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateFd());
                        hitRequest.Session.CurrentMapInstance?.Broadcast(hitRequest.Session, hitRequest.Session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                        hitRequest.Session.CurrentMapInstance?.Broadcast(hitRequest.Session, hitRequest.Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
                        target.Character.DisableBuffs(BuffType.All);

                        if (target.Character.MapInstance == CaligorRaid.CaligorMapInstance)
                        {
                            ServerManager.Instance.AskRevive(target.Character.CharacterId);
                        }
                        else
                        {
                            target.SendPacket(target.Character.GenerateSay(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 11));
                            target.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 0));

                            Observable.Timer(TimeSpan.FromMilliseconds(2000)).Subscribe(o => target.Character.SetSeal());
                        }
                    }
                    else if (target.CurrentMapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                    {
                        if (IceBreaker.AlreadyFrozenPlayers.Contains(target))
                        {
                            IceBreaker.AlreadyFrozenPlayers.Remove(target);
                            target.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_OUT"),
                                    target?.Character?.Name), 0));
                            target.Character.Hp = 1;
                            target.Character.Mp = 1;
                            var respawn = target?.Character?.Respawn;
                            ServerManager.Instance.ChangeMap(target.Character.CharacterId, respawn.DefaultMapId);
                            Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                        }
                        else
                        {
                            isAlive = true;
                            IceBreaker.FrozenPlayers.Add(target);
                            target.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_FROZEN"),
                                    target?.Character?.Name), 0));
                            Task.Run(() =>
                            {
                                target.Character.Hp = (int)target.Character.HPLoad();
                                target.Character.Mp = (int)target.Character.MPLoad();
                                target.SendPacket(target?.Character?.GenerateStat());
                                target.Character.NoMove = true;
                                target.Character.NoAttack = true;
                                target.SendPacket(target?.Character?.GenerateCond());
                                while (IceBreaker.FrozenPlayers.Contains(target))
                                {
                                    target?.CurrentMapInstance?.Broadcast(target?.Character?.GenerateEff(35));
                                    Thread.Sleep(1000);
                                }
                            });
                        }
                    }
                    else if (target.CurrentMapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                    {
                        var rbb = ServerManager.Instance.RainbowBattleMembers.Find(s => s.Session.Contains(Session));

                        rbb.Score += 1;


                        Session.CurrentMapInstance.Broadcast($"msg 0 {hitRequest.Session.Character.Name} killed {target.Character.Name} and won 1 points!");
                        target.Character.RbbDead += 1;
                        RainbowBattleManager.SendFbs(Session.CurrentMapInstance);
                        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                           ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                    }
                    else
                    {
                        hitRequest.Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                hitRequest.Session.Character.Name, target.Character.Name), 10));
                        if (target.Character.IsVehicled)
                        {
                            target.Character.RemoveVehicle();
                        }
                        if (target.Character.Hp <= 0)
                        {
                            if (hitRequest.Session.Character.IsCurrentlyIn1v1Private)
                            {
                                ServerManager.Instance.ChangeMap(target.Character.CharacterId, 1, 80, 116);
                                ServerManager.Instance.ChangeMap(hitRequest.Session.Character.CharacterId, 1, 80, 116);
                                ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg($"{target.Character.Name} has been slain by {hitRequest.Session.Character.Name}", 5));
                                target.Character.DuelLost++;
                                hitRequest.Session.Character.DuelWon++;
                                target.Character.IsCurrentlyIn1v1Private = false;
                                hitRequest.Session.Character.IsCurrentlyIn1v1Private = false;
                                hitRequest.Session.CurrentMapInstance?.Broadcast($"eff 1 {hitRequest.Session.Character.CharacterId} 7179", ReceiverType.All);
                                target.CurrentMapInstance?.Broadcast($"eff 1 {target.Character.CharacterId} 7178", ReceiverType.All);
                                return;
                            }
                            if (hitRequest.Session.Character.IsCurrentlyIn1v1)
                            {
                                ServerManager.Instance.ChangeMap(target.Character.CharacterId, 1, 80, 116);
                                ServerManager.Instance.ChangeMap(hitRequest.Session.Character.CharacterId, 1, 80, 116);
                                ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg($"{target.Character.Name} has been slain by {hitRequest.Session.Character.Name}", 5));
                                target.Character.DuelLost++;
                                hitRequest.Session.Character.DuelWon++;
                                target.Character.IsCurrentlyIn1v1 = false;
                                hitRequest.Session.Character.IsCurrentlyIn1v1 = false;
                                hitRequest.Session.CurrentMapInstance?.Broadcast($"eff 1 {hitRequest.Session.Character.CharacterId} 7179", ReceiverType.All);
                                target.CurrentMapInstance?.Broadcast($"eff 1 {target.Character.CharacterId} 7178", ReceiverType.All);
                                return;
                            }
                            target.Character.ArenaDeath++;
                            hitRequest.Session.Character.ArenaKill++;
                            target.Character.CurrentArenaDeath++;
                            hitRequest.Session.Character.CurrentArenaKill++;
                            hitRequest.Session.CurrentMapInstance?.Broadcast($"eff 1 {hitRequest.Session.Character.CharacterId} 7179", ReceiverType.All);
                            target.CurrentMapInstance?.Broadcast($"eff 1 {target.Character.CharacterId} 7178", ReceiverType.All);
#pragma warning disable 4014
                            MessageExtension.SendSmallHeader(hitRequest.Session, $"You killed {target.Character.Name} | {hitRequest.Session.Character.CurrentArenaKill}:{hitRequest.Session.Character.CurrentArenaDeath}");
                            MessageExtension.SendSmallHeader(hitRequest.Session, $"You have been slain by {hitRequest.Session.Character.Name} | {hitRequest.Session.Character.CurrentArenaKill}:{hitRequest.Session.Character.CurrentArenaDeath}");

                            hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateAscr());
                            target.SendPacket(target.Character.GenerateAscr());
                        }

                        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o => ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                        //ExecuteAfterMilliseconds(2000, () => ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                    }
                }

                battleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                {
                    if (s.Type != (byte)BCardType.CardType.Buff)
                    {
                        s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity);
                    }
                });

                hitRequest.SkillBCards.Where(s =>
                        !s.Type.Equals((byte)CardType.Buff) &&
                        !s.Type.Equals((byte)CardType.Capture) && s.CardId == null).ToList()
                    .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                if (hitmode != 4 && hitmode != 2)
                {
                    battleEntity.BCards.Where(s => s.CastType == 1).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            var b = new Buff((short)s.SecondData, battleEntity.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    foreach (var card in battleEntityDefense.BCards.Where(b => b.CastType == 2))
                    {
                        if (card.Type != (byte)BCardType.CardType.Buff)
                        {
                            continue;
                        }

                        var b = new Buff((short)card.SecondData, battleEntityDefense.Level);
                        if (b.Card == null)
                        {
                            continue;
                        }

                        switch (b.Card?.BuffType)
                        {
                            case BuffType.Bad:
                                card.ApplyBCards(Session.Character.BattleEntity, target.Character.BattleEntity);
                                break;

                            case BuffType.Good:
                            case BuffType.Neutral:
                                card.ApplyBCards(target.Character.BattleEntity, target.Character.BattleEntity);
                                break;
                        }
                    }

                    battleEntityDefense.BCards.Where(s => s.CastType == 1).ForEach(s =>
                    {
                        if (s.Type == (byte)CardType.Buff)
                        {
                            var b = new Buff((short)s.SecondData, battleEntityDefense.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(battleEntity, battleEntityDefense);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(battleEntityDefense, battleEntityDefense);
                                        break;
                                }
                            }
                        }
                    });

                    battleEntityDefense.BCards.Where(s => s.CastType == 0).ForEach(s =>
                    {
                        if (s.Type == (byte)BCardType.CardType.Buff)
                        {
                            var b = new Buff((short)s.SecondData, battleEntityDefense.Level);
                            if (b.Card != null)
                            {
                                switch (b.Card?.BuffType)
                                {
                                    case BuffType.Bad:
                                        s.ApplyBCards(Session.Character.BattleEntity, target.Character.BattleEntity);
                                        break;

                                    case BuffType.Good:
                                    case BuffType.Neutral:
                                        s.ApplyBCards(target.Character.BattleEntity, target.Character.BattleEntity);
                                        break;
                                }
                            }
                        }
                    });

                    hitRequest.SkillBCards.Where(s =>
                            s.Type.Equals((byte)CardType.Buff) &&
                            new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType == BuffType.Bad)
                        .ToList()
                        .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                    hitRequest.SkillBCards.Where(s => s.Type.Equals((byte)CardType.SniperAttack)).ToList()
                        .ForEach(s => s.ApplyBCards(target.Character.BattleEntity, Session.Character.BattleEntity));

                    #region Useless
                    //if (battleEntity?.ShellWeaponEffects != null)
                    //{
                    //    foreach (var shell in battleEntity.ShellWeaponEffects)
                    //    {
                    //        Buff buff = null;
                    //        var chance = (short)(shell.Value >= 100 ? 100 : shell.Value);
                    //        switch (shell.Effect)
                    //        {
                    //            case (byte)ShellWeaponEffectType.Blackout:
                    //                {
                    //                    buff = new Buff(7, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense.ShellArmorEffects
                    //                                                           ?.Find(s =>
                    //                                                                   s.Effect == (byte)ShellArmorEffectType.ReducedStun)
                    //                                                           ?.Value
                    //                                      + battleEntityDefense.ShellArmorEffects?.Find(s =>
                    //                                                                   s.Effect == (byte)ShellArmorEffectType.ReducedAllStun)
                    //                                                           ?.Value
                    //                                      + battleEntityDefense.ShellArmorEffects?.Find(s =>
                    //                                                s.Effect == (byte)ShellArmorEffectType
                    //                                                        .ReducedAllNegativeEffect)?.Value) / 100D);
                    //                }
                    //                break;

                    //            case (byte)ShellWeaponEffectType.DeadlyBlackout:
                    //                {
                    //                    buff = new Buff(66, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense.ShellArmorEffects
                    //                                                           ?.Find(s =>
                    //                                                                   s.Effect == (byte)ShellArmorEffectType.ReducedAllStun)
                    //                                                           ?.Value ?? 0
                    //                          + battleEntityDefense.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllNegativeEffect)?.Value ?? 0) / 100D);
                    //                }
                    //                break;

                    //            case (byte)ShellWeaponEffectType.MinorBleeding:
                    //                {
                    //                    buff = new Buff(1, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                   s.Effect == (byte)ShellArmorEffectType
                    //                                           .ReducedMinorBleeding)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedBleedingAndMinorBleeding)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllBleedingType)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllNegativeEffect)?.Value ?? 0) / 100D);
                    //                }
                    //                break;

                    //            case (byte)ShellWeaponEffectType.Bleeding:
                    //                {
                    //                    buff = new Buff(21, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                   s.Effect == (byte)ShellArmorEffectType
                    //                                           .ReducedBleedingAndMinorBleeding)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllBleedingType)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllNegativeEffect)?.Value ?? 0) / 100D);
                    //                }
                    //                break;

                    //            case (byte)ShellWeaponEffectType.HeavyBleeding:
                    //                {
                    //                    buff = new Buff(42, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                   s.Effect == (byte)ShellArmorEffectType
                    //                                           .ReducedAllBleedingType)?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllNegativeEffect)?.Value ?? 0) / 100D);
                    //                }
                    //                break;

                    //            case (byte)ShellWeaponEffectType.Freeze:
                    //                {
                    //                    buff = new Buff(27, battleEntity.Level);
                    //                    chance -= (short)((battleEntityDefense?.ShellArmorEffects
                    //                                                           ?.Find(s =>
                    //                                                                   s.Effect == (byte)ShellArmorEffectType.ReducedFreeze)
                    //                                                           ?.Value ?? 0
                    //                          + battleEntityDefense?.ShellArmorEffects?.Find(s =>
                    //                                    s.Effect == (byte)ShellArmorEffectType
                    //                                            .ReducedAllNegativeEffect)?.Value ?? 0) / 100D);
                    //                }
                    //                break;
                    //        }

                    //        if (buff == null)
                    //        {
                    //            break;
                    //        }

                    //        if (ServerManager.RandomNumber() < chance || chance == 100)
                    //        {
                    //            target.Character.AddBuff(buff, battleEntity);
                    //        }
                    //    }
                    //}
                    #endregion
                }

                if (hitmode != 2)
                {
                    switch (hitRequest.TargetHitType)
                    {
                        case TargetHitType.SingleTargetHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum,
                                (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleTargetHitCombo:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum,
                                (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                hitRequest.SkillCombo.Animation,
                                hitRequest.SkillCombo.Effect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleAOETargetHit:
                            if (hitRequest.ShowTargetHitAnimation)
                            {
                                if (hitRequest.Skill.SkillVNum == 1085 || hitRequest.Skill.SkillVNum == 1091 ||
                                    hitRequest.Skill.SkillVNum == 1060 || hitRequest.Skill.SkillVNum == 718 || hitRequest.Skill.SkillVNum == 1607)
                                {
                                    hitRequest.Session.Character.PositionX = target.Character.PositionX;
                                    hitRequest.Session.Character.PositionY = target.Character.PositionY;
                                    hitRequest.Session.CurrentMapInstance.Broadcast(hitRequest.Session.Character
                                        .GenerateTp());
                                }

                                hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                    UserType.Player, hitRequest.Session.Character.CharacterId, 1,
                                    target.Character.CharacterId,
                                    hitRequest.Skill.SkillVNum,
                                    (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                    isAlive,
                                    (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage,
                                    hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                            }
                            else
                            {
                                switch (hitmode)
                                {
                                    case 1:
                                    case 4:
                                        hitmode = 7;
                                        break;

                                    case 2:
                                        hitmode = 2;
                                        break;

                                    case 3:
                                        hitmode = 6;
                                        break;

                                    default:
                                        hitmode = 5;
                                        break;
                                }

                                hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                    UserType.Player, hitRequest.Session.Character.CharacterId, 1,
                                    target.Character.CharacterId,
                                    -1,
                                    (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                    isAlive,
                                    (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage,
                                    hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                            }

                            break;

                        case TargetHitType.AOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                case 4:
                                    hitmode = 7;
                                    break;

                                case 2:
                                    hitmode = 2;
                                    break;

                                case 3:
                                    hitmode = 6;
                                    break;

                                default:
                                    hitmode = 5;
                                    break;
                            }

                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum,
                                (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.ZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum,
                                (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.MapX, hitRequest.MapY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SpecialZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum,
                                (short)(hitRequest.Skill.Cooldown - (hitRequest.Skill.Cooldown * ((cooldownReduction / 100D)))),
                                hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        default:
                            //LOGGER("[HANDLER] TargetHitType Handler wasn't implemented");
                            break;
                    }
                }
                else
                {
                    if (target != null)
                    {
                        hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    }
                }
            }
            else
            {
                // monster already has been killed, send cancel
                if (target != null)
                {
                    hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                }
            }
        }

        public static void RageLimitReached(ClientSession Session)
        {
            if (Session.Character.WaterfallBerserkerRage >= 100)
            {
                Session.Character.WaterfallBerserkerRage = 0;
            }
        }

        public static void TargetHit(this ClientSession Session, int castingId, UserType targetType, int targetId, bool isPvp = false)
        {
            var shouldCancel = true;
            var isSacrificeSkill = false;

            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"),0));
                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                return;
            }


            var skills = Session.Character.GetSkills();

            if (skills != null)
            {
                var ski = skills.FirstOrDefault(s =>
                    s.Skill?.CastId == castingId &&
                    (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == (byte)SkillType.CharacterSKill));

                var ski3 = skills.FirstOrDefault(s =>
                    s.Skill?.CastId == castingId &&
                    (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == (byte)SkillType.Emote));

                if (castingId != 0)
                {
                    Session.SendPacket("ms_c 0");

                    foreach (var qslot in Session.Character.GenerateQuicklist())
                    {
                        Session.SendPacket(qslot);
                    }
                }

                if (ski3 != null)
                {
                    if (ski3.SkillVNum == 2000)
                    {
                        Session.SendPacket("info This worked");
                        Session.CurrentMapInstance?.Broadcast($"eff 1 {Session.Character.CharacterId} 834", ReceiverType.All);
                    }
                }


                #region Scout
                if (ski != null)
                {
                    if (ski.SkillVNum == 1122)
                    {
                        Session.CurrentMapInstance?.Broadcast($"eff 1 {Session.Character.CharacterId} 7134", ReceiverType.All);
                    }

                    if (ski.SkillVNum == 1117)
                    {
                        Session.SendPacket("ob_ar");
                        Thread.Sleep(800);
                        Session.SendPacket("ob_ar");
                        Thread.Sleep(200);
                        Session.SendPacket("ob_ar");
                    }

                    if (ski.SkillVNum == 1119)
                    {
                        Session.CurrentMapInstance?.Broadcast($"eff 1 {Session.Character.CharacterId} 8134", ReceiverType.All);
                    }
                }
                #endregion

                #region Seer
                if (ski != null)
                {
                    if (ski.SkillVNum == 1135)
                    {
                        Session.Character.AddBuff(new Buff(665, Session.Character.Level), Session.Character.BattleEntity);
                    }
                }
                #endregion

                #region Battle Monk
                if (ski != null)
                {
                    if (ski.SkillVNum == 1096)
                    {
                        Session.Character.AddBuff(new Buff(534, Session.Character.Level), Session.Character.BattleEntity);
                    }
                }
                #endregion

                #region Holy Mage
                if (ski.SkillVNum == 872)
                {
                    Session.Character.AddBuff(new Buff(4020, Session.Character.Level), Session.Character.BattleEntity);
                }
                #endregion

                #region SP9
                //ftpt 1 90 100
                if (ski != null)
                {
                    switch (ski.SkillVNum)
                    {
                        case 1352:
                            Session.Character.WaterfallBerserkerRage += 2;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1353:
                            Session.Character.WaterfallBerserkerRage += 10;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1354:
                            Session.Character.WaterfallBerserkerRage += 10;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1382:
                            Session.Character.WaterfallBerserkerRage += 10;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1384:
                            Session.Character.WaterfallBerserkerRage += 10;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1385:
                            Session.Character.WaterfallBerserkerRage += 5;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1387:
                            Session.Character.WaterfallBerserkerRage += 10;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            RageLimitReached(Session);
                            break;

                        case 1389:
                            Session.Character.WaterfallBerserkerRage = 0;
                            Session.Character.GenerateWaterfallBerserkerRage();
                            break;
                    }
                    if (Session.Character.WaterfallBerserkerRage >= 20)
                    {
                        if (Session.Character.HasBuff(922))
                        {
                            return;
                        }
                        if (!Session.Character.HasBuff(922))
                        {
                            Session.Character.AddBuff(new Buff(922, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(923);
                            Session.Character.RemoveBuff(924);
                            Session.Character.RemoveBuff(925);
                        }
                    }
                    if (Session.Character.WaterfallBerserkerRage >= 40)
                    {
                        if (Session.Character.HasBuff(923))
                        {
                            return;
                        }
                        if (!Session.Character.HasBuff(923))
                        {
                            Session.Character.AddBuff(new Buff(923, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(922);
                            Session.Character.RemoveBuff(924);
                            Session.Character.RemoveBuff(925);
                        }
                    }
                    if (Session.Character.WaterfallBerserkerRage >= 20)
                    {
                        if (Session.Character.HasBuff(924))
                        {
                            return;
                        }
                        if (!Session.Character.HasBuff(924))
                        {
                            Session.Character.AddBuff(new Buff(924, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(922);
                            Session.Character.RemoveBuff(923);
                            Session.Character.RemoveBuff(925);
                        }
                    }
                    if (Session.Character.WaterfallBerserkerRage >= 20)
                    {
                        if (Session.Character.HasBuff(925))
                        {
                            return;
                        }
                        if (!Session.Character.HasBuff(925))
                        {
                            Session.Character.AddBuff(new Buff(925, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(922);
                            Session.Character.RemoveBuff(923);
                            Session.Character.RemoveBuff(924);
                        }
                    }
                }
                #endregion

                #region SP10
                if (ski != null)
                {
                    switch (ski.SkillVNum)
                    {
                        case 1732:
                            Session.Character.Heat += 2;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1733:
                            Session.Character.Heat += 5;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1734:
                            Session.Character.Heat = 0;
                            Session.Character.Heat += 20;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1735:
                            Session.Character.Heat += 20;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1736:
                            if (Session.Character.Heat < 40)
                            {
                                Session.Character.Heat = 0;
                                Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                                return;
                            }
                            Session.Character.Heat -= 40;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        //VNum 1737 is a buff which doesnt consume nor add Heat Points

                        case 1738:
                            Session.Character.Heat += 30;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1739:
                            if (Session.Character.Heat < 40)
                            {
                                Session.Character.Heat = 0;
                                Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                                return;
                            }
                            Session.Character.Heat -= 40;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1740:
                            Session.Character.Heat -= 30;
                            if (Session.Character.Heat < 30)
                            {
                                Session.Character.Heat = 0;
                                Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                                return;
                            }
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1741:
                            if (Session.Character.Heat < 50)
                            {
                                Session.Character.Heat = 0;
                                Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                                return;
                            }
                            Session.Character.Heat -= 50;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;

                        case 1742:
                            Session.Character.Heat = 0;
                            Session.SendPacket($"ftpt 2 {Session.Character.Heat} 100");
                            break;
                    }
                }

                if (Session.Character.Heat > 1)
                {
                    if (Session.Character.Heat >= 20)
                    {
                        if (!Session.Character.HasBuff(1463))
                        {
                            Session.Character.AddBuff(new Buff(1463, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(1464);
                            Session.Character.RemoveBuff(1465);
                        }
                    }
                    if (Session.Character.Heat >= 50)
                    {
                        if (!Session.Character.HasBuff(1464))
                        {
                            Session.Character.AddBuff(new Buff(1464, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(1463);
                            Session.Character.RemoveBuff(1465);
                        }
                    }
                    if (Session.Character.Heat >= 70)
                    {
                        if (!Session.Character.HasBuff(1465))
                        {
                            Session.Character.AddBuff(new Buff(1465, Session.Character.Level), Session.Character.BattleEntity);
                            Session.Character.RemoveBuff(1464);
                        }
                    }
                    if (Session.Character.Heat >= 100)
                    {
                        if (!Session.Character.HasBuff(1466))
                        {
                            Session.Character.AddBuff(new Buff(1466, Session.Character.Level), Session.Character.BattleEntity);
                        }
                    }
                }
                #endregion

                if (ski != null)
                {
                    // We will reinstantiate the skill so we can edit cooldown without modifying anything
                    // Note: I did it like this because I didn't know MapMonster.cs had the cooldown reset calculation... Have to re-do it later.
                    //ski.ReinstantiateSkill();


                    if (!Session.Character.WeaponLoaded(ski) || !ski.CanBeUsed())
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));

                        return;
                    }

                    Session.Character.LastSkillType = ski.Skill;
                    if (ski.SkillVNum == 656)
                    {
                        Session.Character.RemoveUltimatePoints(2000);
                    }
                    else if (ski.SkillVNum == 657)
                    {
                        Session.Character.RemoveUltimatePoints(1000);
                    }
                    else if (ski.SkillVNum == 658 || ski.SkillVNum == 659)
                    {
                        Session.Character.RemoveUltimatePoints(3000);
                    }
                    else if (ski.SkillVNum == 797) // Throw fishing rod
                    {
                        if (Session.Character.IsFishing || Session.Character.IsBiting) // fishing buff
                        {
                            Session.SendPacket(Session.Character.GenerateSay("I'm fishing already.", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            return;
                        }

                        var fishingSpot = ServerManager.Instance.GetFishingSpot(Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);

                        if (fishingSpot == null)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You cannot fish here!", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        short baitVnum = 2614;
                        short fishingLineVnum = 2615;

                        var baits = Session.Character.Inventory.CountItem(baitVnum);
                        var fishingLines = Session.Character.Inventory.CountItem(fishingLineVnum);

                        if (baits < 1)
                        {
                            Session.SendPacket(Session.Character.GenerateSay($"You need at least 1x {ServerManager.GetItem(baitVnum).Name} to fish!", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        if (fishingLines < 1)
                        {
                            Session.SendPacket(Session.Character.GenerateSay($"You need at least 1x {ServerManager.GetItem(fishingLineVnum).Name} to fish!", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        var spotInfo = ServerManager.Instance.FishingSpots.FirstOrDefault(s => s.Key.MapId == Session.Character.MapId && s.Key.MapX == Session.Character.MapX && s.Key.MapY == Session.Character.MapY);

                        if (spotInfo.Equals(default(KeyValuePair<FishingPositionDto, List<FishingInformationsDto>>)))
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You cannot fish here!", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        var sp = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                        if (sp == null || !Session.Character.UseSp)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("Please wear the fisherman.", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        if (spotInfo.Key.MinLevel > sp.SpLevel)
                        {
                            Session.SendPacket(Session.Character.GenerateSay($"Your specialist card must be at least level {spotInfo.Key.MinLevel}", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                            return;
                        }

                        if (spotInfo.Key.Direction != Session.Character.Direction)
                        {
                            Session.Character.BeforeDirection = Session.Character.Direction;
                            Session.Character.Direction = (byte)spotInfo.Key.Direction;
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateDir());
                        }

                        Session.SendPacket("pdti 0");
                        Session.Character.Inventory.RemoveItemAmount(baitVnum);
                    }
                    else if (ski.SkillVNum == 798)
                    {
                        if (!Session.Character.IsFishing)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You must be fishing to use this skill.", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            return;
                        }

                        if (!Session.Character.IsBiting)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("Bad luck... Unfortunately nothing bit", 0));
                            Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 0, Session.Character.CharacterId));
                            Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 44, Session.Character.CharacterId));
                            Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(19, 1, Session.Character.CharacterId, 2436));
                            Session.Character.RemoveBuff(850);
                            Session.Character.IsBiting = false;
                        }
                        else
                        {
                            var fishingSpot = ServerManager.Instance.GetFishingSpot(Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);

                            if (fishingSpot == null)
                            {
                                Session.SendPacket(Session.Character.GenerateSay("You cannot fish here!", 0));
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                Session.Character.RemoveBuff(850);
                                Session.Character.IsBiting = false;
                                return;
                            }

                            sendFishReward:
                            var rnd = ServerManager.RandomDouble();
                            var fishes = fishingSpot.Where(s => s.IsFish).GroupBy(s => s.Probability).OrderBy(x => x.Key);
                            short fishFound = -1;
                            short length = 0;
                            foreach (var grouping in fishes)
                            {
                                double prob = (double)grouping.Key / 10;

                                if (rnd > prob)
                                {

                                }

                                var selectedReward = grouping.ElementAt(ServerManager.RandomNumber(0, grouping.Count()));
                                fishFound = selectedReward.FishVNum;
                                length = ServerManager.RandomNumber<short>((short)selectedReward.MinFishLength, (short)(selectedReward.MaxFishLength + 1));
                                break;
                            }

                            if (fishFound == -1)
                            {
                                var retry = ServerManager.RandomNumber() < 80;

                                if (retry)
                                {
                                    goto sendFishReward;
                                }

                                Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 0, Session.Character.CharacterId));
                                Session.Character.IsBiting = false;
                                Session.Character.RemoveBuff(850);
                                Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 0, Session.Character.CharacterId));
                                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(x =>
                                {
                                    if (Session?.Character == null)
                                    {
                                        return;
                                    }

                                    Session.SendPacket(Session.Character.GenerateSay("Oh, my fishing line broke!", 0));
                                    Session.Character.Inventory.RemoveItemAmount(2615);
                                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.SkillVNum, ski.Skill.Cooldown, 45, -1, 0, 0, true, 100, 0, -1, 0));
                                    Session.Character.IsFishing = false;
                                });
                            }
                            else
                            {
                                Session.Character.RemoveBuff(850);
                                Session.Character.IsBiting = false;

                                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(x =>
                                {
                                    if (Session?.Character == null)
                                    {
                                        return;
                                    }

                                    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.SkillVNum, ski.Skill.Cooldown, 42, -1, 0, 0, true, 100, 0, -1, 0));
                                });

                                Observable.Timer(TimeSpan.FromSeconds(4)).Subscribe(x =>
                                {
                                    if (Session?.Character == null)
                                    {
                                        return;
                                    }

                                    var itm = Inventory.InstantiateItemInstance(fishFound, Session.Character.CharacterId);
                                    var inv = Session.Character.Inventory.AddToInventory(itm).FirstOrDefault();
                                    Session.SendPacket("pdti 0");
                                    Session.SendPacket(Session.Character.GenerateSay($"You have received this item - {ServerManager.GetItem(fishFound).Name} x1", 11));
                                    Session.SendPacket($"pdti 17 {fishFound} 1 {length} -1 -1");
                                    Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(19, 1, Session.Character.CharacterId, 1324));
                                    Session.SendPacket(CharacterExtension.GenerateFishPacket(Session, FishPacketType.Fishing, fishFound, length));

                                    var sp = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                                    if (sp != null && Session.Character.UseSp && sp.SpLevel < GameConfiguration.MaxSPLevel)
                                    {
                                        sp.XP += (long)(CharacterHelper.SPXPData[sp.SpLevel - 1] / 8);
                                        Session.Character.GenerateSpXpLevelUp(sp);
                                        Session.SendPacket(Session.Character.GenerateLev());
                                    }

                                    Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 0, Session.Character.CharacterId));
                                    Session.Character.IsBiting = false;
                                    Session.Character.RemoveBuff(850);
                                });
                            }
                        }
                    }
                    else if (ski.SkillVNum == 968)
                    {
                        var fishingSpots = ServerManager.Instance.GetFishingSpot(Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
                        if (Session.Character.FishingSpotsMapId == 1 && fishingSpots == null && Session.Character.FishingSpotsMapX.IsBetween<short>(78, 81))
                        {
                            Session.SendPacket(Session.Character.GenerateSay("I can't use that if there is no fishing spots close to me.", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            return;
                        }

                        if (Session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance &&
                            Session.Character.MapInstance.MapInstanceType != MapInstanceType.NormalInstance)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("You can't use that now.", 0));
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                            return;
                        }


                        if (Session.Character.FishingSpotsMapId == 1 && Session.Character.FishingSpotsMapX.IsBetween<short>(78, 81))
                        {
                            short mapId = Session.Character.FishingSpotsMapId;
                            short mapX = Session.Character.FishingSpotsMapX;
                            short mapY = Session.Character.FishingSpotsMapY;
                            Session.Character.FishingSpotsMapId = Session.Character.MapId;
                            Session.Character.FishingSpotsMapX = Session.Character.MapX;
                            Session.Character.FishingSpotsMapY = Session.Character.MapY;
                            ServerManager.Instance.ChangeMap(Session.Character.CharacterId, mapId, mapX, mapY);
                            return;
                        }

                        short mapIdd = Session.Character.FishingSpotsMapId;
                        short mapXX = Session.Character.FishingSpotsMapX;
                        short mapYY = Session.Character.FishingSpotsMapY;
                        Session.Character.FishingSpotsMapId = 1;
                        Session.Character.FishingSpotsMapX = ServerManager.RandomNumber<short>(78, 81);
                        Session.Character.FishingSpotsMapY = ServerManager.RandomNumber<short>(114, 118);
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, mapIdd, mapXX, mapYY);
                    }

                    if (Session.Character.LastSkillComboUse > DateTime.Now
                     && ski.SkillVNum != SkillHelper.GetOriginalSkill(ski.Skill)?.SkillVNum)
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        return;
                    }

                    BattleEntity targetEntity = null;

                    switch (targetType)
                    {
                        case UserType.Player:
                            {
                                targetEntity = ServerManager.Instance.GetSessionByCharacterId(targetId)?.Character
                                    ?.BattleEntity;
                            }
                            break;

                        case UserType.Npc:
                            {
                                targetEntity = Session.Character.MapInstance?.Npcs?.ToList()
                                                   .FirstOrDefault(n => n.MapNpcId == targetId)?.BattleEntity
                                               ?? Session.Character.MapInstance?.Sessions
                                                   ?.Where(s => s?.Character?.Mates != null)
                                                   .SelectMany(s => s.Character.Mates)
                                                   .FirstOrDefault(m => m.MateTransportId == targetId)?.BattleEntity;
                            }
                            break;

                        case UserType.Monster:
                            {
                                targetEntity = Session.Character.MapInstance?.Monsters?.ToList()
                                    .FirstOrDefault(m => m.Owner?.Character == null && m.MapMonsterId == targetId)
                                    ?.BattleEntity;
                            }
                            break;
                    }

                    if (targetEntity == null)
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2));

                        return;
                    }

                    foreach (var bc in ski.GetSkillBCards().ToList().Where(s =>
                        s.Type.Equals((byte)BCardType.CardType.MeditationSkill)
                        && (!s.SubType.Equals((byte)AdditionalTypes.MeditationSkill.CausingChance) ||
                            SkillHelper.IsCausingChance(ski.SkillVNum))))
                    {
                        shouldCancel = false;

                        if (bc.SubType.Equals((byte)AdditionalTypes.MeditationSkill.Sacrifice))
                        {
                            isSacrificeSkill = true;
                            if (targetEntity == Session.Character.BattleEntity || targetEntity.MapMonster != null ||
                                targetEntity.MapNpc != null)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("INVALID_TARGET"), 0));
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));

                                return;
                            }
                        }

                        bc.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                    }

                    if (ski.Skill.SkillVNum == 1098 && ski.GetSkillBCards().FirstOrDefault(s =>
                                s.Type.Equals((byte)BCardType.CardType.SpecialisationBuffResistance) &&
                                s.SubType.Equals((byte)AdditionalTypes.SpecialisationBuffResistance.RemoveBadEffects))
                            is
                            BCard RemoveBadEffectsBcard)
                    {
                        if (Session.Character.BattleEntity.BCardDisposables[RemoveBadEffectsBcard.BCardId] != null)
                        {
                            Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(castingId, 300));
                            ski.LastUse = DateTime.Now.AddSeconds(29);
                            Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                            {
                                var
                                        skill = Session.Character.GetSkills().Find(s =>
                                                s.Skill?.CastId
                                             == castingId &&
                                                (s.Skill?.UpgradeSkill == 0 ||
                                                 s.Skill?.SkillType == (byte)SkillType.CharacterSKill));
                                if (skill != null && skill.LastUse <= DateTime.Now)
                                {
                                    Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                                }
                            });
                            RemoveBadEffectsBcard.ApplyBCards(Session.Character.BattleEntity,
                                    Session.Character.BattleEntity);
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));

                            return;
                        }
                    }

                    double cooldownReduction = Session.Character.GetBuff(BCardType.CardType.Morale,
                                                       (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0] +
                                               Session.Character.GetBuff(BCardType.CardType.Casting,
                                                       (byte)AdditionalTypes.Casting.EffectDurationIncreased)[0];

                    BattleEntity mCd = new BattleEntity(Session.Character, ski.Skill);
                    if (mCd.AttackType == AttackType.Magical)
                    {
                        cooldownReduction += Session.Character.GetBuff(BCardType.CardType.AbsorbedSpirit,
                                                   (byte)AdditionalTypes.AbsorbedSpirit.MagicCooldownDecreased)[0];
                    }

                    var increaseEnemyCooldownChance = Session.Character.GetBuff(BCardType.CardType.DarkCloneSummon,
                        (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                    if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                    {
                        cooldownReduction -= increaseEnemyCooldownChance[1];
                    }

                    var mpCost = ski.MpCost();
                    short hpCost = 0;

                    mpCost = (short)(mpCost * ((100 - Session.Character.CellonOptions
                                                     .Where(s => s.Type == CellonOptionType.MPUsage)
                                                     .Sum(s => s.Value)) / 100D));

                    if (Session.Character.GetBuff(BCardType.CardType.HealingBurningAndCasting,
                            (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int
                        HPDecreasedByConsumingMP)
                    {
                        if (HPDecreasedByConsumingMP < 0)
                        {
                            var amountDecreased = ski.MpCost() * HPDecreasedByConsumingMP / 100;
                            hpCost = (short)amountDecreased;
                            mpCost -= (short)amountDecreased;
                        }
                    }

                    if (Session.Character.Mp >= mpCost && Session.Character.Hp > hpCost &&
                        Session.HasCurrentMapInstance)
                    {
                        if (!Session.Character.HasGodMode)
                        {
                            Session.Character.DecreaseMp(ski.MpCost());
                        }

                        ski.LastUse = DateTime.Now;

                        // We save the reduced cooldown amount for using it later
                        var reducedCooldown = (ski.Skill.Cooldown * (cooldownReduction / 100D));

                        // We will check if there's a cooldown reduction in queue
                        if (cooldownReduction != 0)
                        {
                            ski.Skill.Cooldown = (short)(ski.Skill.Cooldown - reducedCooldown);
                            ski.LastUse = ski.LastUse.AddMilliseconds((reducedCooldown) * -1 * 100);
                        }



                        Session.Character.PyjamaDead = ski.SkillVNum == 801;

                        // Area on attacker
                        if (ski.Skill.TargetType == 1 && ski.Skill.HitType == 1)
                        {
                            if (Session.Character.MapInstance.MapInstanceType ==
                                MapInstanceType.TalentArenaMapInstance && !Session.Character.MapInstance.IsPVP)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                return;
                            }

                            if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                            {
                                Session.SendPackets(Session.Character.GenerateQuicklist());
                            }

                            Session.SendPacket(Session.Character.GenerateStat());
                            var skillinfo = Session.Character.Skills.FirstOrDefault(s =>
                                s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                            && s.Skill.SkillType == 2);

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, targetType, targetId,
                                ski.Skill.CastAnimation, skillinfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                ski.Skill.SkillVNum));

                            var skillEffect = skillinfo?.Skill.Effect ?? ski.Skill.Effect;

                            if (Session.Character.BattleEntity.HasBuff(BCardType.CardType.FireCannoneerRangeBuff,
                                    (byte)AdditionalTypes.FireCannoneerRangeBuff.AOEIncreased) &&
                                ski.Skill.Effect == 4569)
                            {
                                skillEffect = 4572;
                            }

                            #region SP2 MA
                            if (ski.Skill.SkillVNum == 1618)
                            {
                                foreach (BCard bc in ski.Skill.BCards.Where(s => s.Type == (byte)CardType.Summons && s.SubType == (byte)AdditionalTypes.Summons.Summons))
                                {
                                    bc.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity);
                                }
                            }
                            #endregion

                            var targetRange = ski.TargetRange();

                            if (targetRange != 0)
                            {
                                ski.GetSkillBCards().Where(s =>
                                           s.Type.Equals((byte)BCardType.CardType.Buff) &&
                                           new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType ==
                                           BuffType.Good
                                        || s.Type.Equals((byte)BCardType.CardType.SpecialEffects2) &&
                                           s.SubType.Equals((byte)AdditionalTypes.SpecialEffects2.TeleportInRadius))
                                   .ToList()
                                   .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity,
                                           Session.Character.BattleEntity, partnerBuffLevel: ski.TattooLevel));
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                            Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                                            (short)(ski.Skill.Cooldown),
                                            ski.Skill.AttackAnimation,
                                            skillEffect, Session.Character.PositionX,
                                            Session.Character.PositionY, true,
                                            (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -2,
                                            (byte)(ski.Skill.SkillType - 1)));

                            if (targetRange != 0)
                            {
                                foreach (var character in ServerManager.Instance.Sessions.Where(s =>
                                    s.CurrentMapInstance == Session.CurrentMapInstance
                                    && s.Character.CharacterId != Session.Character.CharacterId
                                    && s.Character.IsInRange(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.TargetRange())))
                                {
                                    if (Session.Character.BattleEntity.CanAttackEntity(character.Character.BattleEntity)
                                    )
                                    {
                                        Session.PvpHit(
                                                new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                                        skillBCards: ski.GetSkillBCards()),
                                                character);
                                    }
                                }

                                foreach (var mon in Session.CurrentMapInstance
                                                           .GetMonsterInRangeList(Session.Character.PositionX, Session.Character.PositionY,
                                                                   ski.TargetRange()).Where(s =>
                                                                   Session.Character.BattleEntity.CanAttackEntity(s.BattleEntity)))
                                {
                                    lock (mon._onHitLockObject)
                                    {
                                        mon.OnReceiveHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                                skillinfo?.Skill.Effect ?? ski.Skill.Effect));
                                    }
                                }

                                foreach (var mate in Session.CurrentMapInstance
                                                            .GetListMateInRange(Session.Character.PositionX, Session.Character.PositionY,
                                                                    ski.TargetRange()).Where(s =>
                                                                    Session.Character.BattleEntity.CanAttackEntity(s.BattleEntity)))
                                {
                                    mate.HitRequest(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                            skillinfo?.Skill.Effect ?? ski.Skill.Effect,
                                            skillBCards: ski.GetSkillBCards()));
                                }
                            }
                        }
                        else if (ski.Skill.TargetType == 2 && ski.Skill.HitType == 0 || isSacrificeSkill)
                        {
                            ConcurrentBag<ArenaTeamMember> team = null;
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                            {
                                team = ServerManager.Instance.ArenaTeams.ToList()
                                                    .FirstOrDefault(s => s.Any(o => o.Session == Session));
                            }

                            if (Session.Character.BattleEntity.CanAttackEntity(targetEntity)
                             || team != null && team.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType !=
                                team.FirstOrDefault(s => s.Session == targetEntity.Character.Session)?.ArenaTeamType)
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }

                            if (Session.Character.MapInstance == ServerManager.Instance.ArenaInstance &&
                                targetEntity.Mate?.Owner != Session.Character &&
                                targetEntity != Session.Character.BattleEntity &&
                                (Session.Character.Group == null ||
                                 !Session.Character.Group.IsMemberOfGroup(targetEntity.MapEntityId)))
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }

                            if (Session.Character.MapInstance == ServerManager.Instance.FamilyArenaInstance &&
                                targetEntity.Mate?.Owner != Session.Character &&
                                targetEntity != Session.Character.BattleEntity &&
                                Session.Character.Family !=
                                (targetEntity.Character?.Family ?? targetEntity.Mate?.Owner.Family ??
                                        targetEntity.MapMonster?.Owner?.Character?.Family))
                            {
                                targetEntity = Session.Character.BattleEntity;
                            }

                            if (targetEntity.Character != null && targetEntity.Character.IsSitting)
                            {
                                targetEntity.Character.IsSitting = false;
                                Session.CurrentMapInstance?.Broadcast(targetEntity.Character.GenerateRest());
                            }

                            if (targetEntity.Mate != null && targetEntity.Mate.IsSitting)
                            {
                                Session.CurrentMapInstance?.Broadcast(targetEntity.Mate.GenerateRest(false));
                            }

                            ski.GetSkillBCards().ToList()
                               .Where(s => !s.Type.Equals((byte)BCardType.CardType.MeditationSkill)).ToList()
                               .ForEach(s => s.ApplyBCards(targetEntity, Session.Character.BattleEntity,
                                       partnerBuffLevel: ski.TattooLevel));

                            targetEntity.MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, targetEntity.UserType, targetEntity.MapEntityId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));
                            targetEntity.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, (byte)targetEntity.UserType, targetEntity.MapEntityId,
                                ski.Skill.SkillVNum,
                                (short)(ski.Skill.Cooldown),
                                ski.Skill.AttackAnimation, ski.Skill.Effect, targetEntity.PositionX,
                                targetEntity.PositionY, true,
                                (int)(targetEntity.Hp / targetEntity.HPLoad() * 100), 0, -1,
                                (byte)(ski.Skill.SkillType - 1)));
                        }
                        else if (ski.Skill.TargetType == 1 && ski.Skill.HitType != 1)
                        {
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, UserType.Player, Session.Character.CharacterId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));

                            if (ski.Skill.CastEffect != 0)
                            {
                                Thread.Sleep(ski.Skill.CastTime * 100);
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                            Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                                            (short)(ski.Skill.Cooldown),
                                            ski.Skill.AttackAnimation, ski.Skill.Effect,
                                            Session.Character.PositionX, Session.Character.PositionY, true,
                                            (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -1,
                                            (byte)(ski.Skill.SkillType - 1)));


                            // test?
                            if (ski.SkillVNum == 1330)
                            {
                                if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    return;
                                }
                            }

                            if (ski.SkillVNum != 1330)
                            {
                                switch (ski.Skill.HitType)
                                {
                                    case 0:
                                    case 4:
                                        if (Session.Character.Buff.FirstOrDefault(s =>
                                                        s.Card.BCards.Any(b =>
                                                                b.Type == (byte)BCardType.CardType.FalconSkill &&
                                                                b.SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide))) is Buff
                                                FalconHideBuff)
                                        {
                                            Session.Character.RemoveBuff(FalconHideBuff.Card.CardId);
                                            Session.Character.AddBuff(new Buff(560, Session.Character.Level),
                                                    Session.Character.BattleEntity);
                                        }

                                        break;

                                    case 2:
                                        ConcurrentBag<ArenaTeamMember> team = null;
                                        if (Session.Character.MapInstance.MapInstanceType ==
                                            MapInstanceType.TalentArenaMapInstance)
                                        {
                                            team = ServerManager.Instance.ArenaTeams.ToList()
                                                                .FirstOrDefault(s => s.Any(o => o.Session == Session));
                                        }

                                        var clientSessions =
                                                Session.CurrentMapInstance.Sessions?.Where(s =>
                                                        s.Character.CharacterId != Session.Character.CharacterId &&
                                                        s.Character.IsInRange(Session.Character.PositionX,
                                                                Session.Character.PositionY, ski.TargetRange()));
                                        if (clientSessions != null)
                                        {
                                            foreach (var target in clientSessions)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.Character
                                                                                                          .BattleEntity)
                                                 && (team == null ||
                                                     team.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType ==
                                                     team.FirstOrDefault(s => s.Session == target.Character.Session)
                                                         ?.ArenaTeamType))
                                                {
                                                    foreach (var s in ski.Skill.BCards.Where(s =>
                                                                                 !s.Type.Equals((byte)BCardType.CardType.MeditationSkill))
                                                                         .ToList())
                                                    {
                                                        if (s.Type != (short)BCardType.CardType.Buff)
                                                        {
                                                            s.ApplyBCards(target.Character.BattleEntity,
                                                                    Session.Character.BattleEntity);
                                                            continue;
                                                        }

                                                        switch (Session.CurrentMapInstance.MapInstanceType)
                                                        {
                                                            case MapInstanceType.Act4Berios:
                                                            case MapInstanceType.Act4Calvina:
                                                            case MapInstanceType.Act4Hatus:
                                                            case MapInstanceType.Act4Morcos:
                                                                var bf = new Buff((short)s.SecondData, 0);
                                                                switch (bf.Card?.BuffType)
                                                                {
                                                                    case BuffType.Bad:
                                                                        s.ApplyBCards(target.Character.BattleEntity,
                                                                                Session.Character.BattleEntity,
                                                                                partnerBuffLevel: ski.TattooLevel);
                                                                        break;

                                                                    case BuffType.Good:
                                                                    case BuffType.Neutral:
                                                                        if (Session.Character.Faction ==
                                                                            target.Character.Faction)
                                                                        {
                                                                            s.ApplyBCards(target.Character.BattleEntity,
                                                                                    Session.Character.BattleEntity,
                                                                                    partnerBuffLevel: ski.TattooLevel);
                                                                        }

                                                                        break;
                                                                }

                                                                break;

                                                            case MapInstanceType.ArenaInstance:
                                                                var b = new Buff((short)s.SecondData, 0);
                                                                switch (b.Card?.BuffType)
                                                                {
                                                                    case BuffType.Bad:
                                                                        s.ApplyBCards(target.Character.BattleEntity,
                                                                                Session.Character.BattleEntity,
                                                                                partnerBuffLevel: ski.TattooLevel);
                                                                        break;

                                                                    case BuffType.Good:
                                                                    case BuffType.Neutral:
                                                                        if (Session.Character.Group?.GroupType ==
                                                                            GroupType.Group &&
                                                                            Session.Character.Group.IsMemberOfGroup(
                                                                                    target.Character.CharacterId))
                                                                        {
                                                                            s.ApplyBCards(target.Character.BattleEntity,
                                                                                    Session.Character.BattleEntity,
                                                                                    partnerBuffLevel: ski.TattooLevel);
                                                                        }
                                                                        else
                                                                        {
                                                                            s.ApplyBCards(
                                                                                    Session.Character.BattleEntity,
                                                                                    Session.Character.BattleEntity,
                                                                                    partnerBuffLevel: ski.TattooLevel);
                                                                        }

                                                                        break;
                                                                }

                                                                break;

                                                            default:
                                                                s.ApplyBCards(target.Character.BattleEntity,
                                                                        Session.Character.BattleEntity,
                                                                        partnerBuffLevel: ski.TattooLevel);
                                                                break;
                                                        }
                                                    }

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(
                                                            UserType.Player,
                                                            Session.Character.CharacterId, 1, target.Character.CharacterId,
                                                            ski.Skill.SkillVNum,
                                                            (short)(ski.Skill.Cooldown),
                                                            ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                            target.Character.PositionX, target.Character.PositionY, true,
                                                            (int)(target.Character.Hp / target.Character.HPLoad() * 100),
                                                            0, -1,
                                                            (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<Mate> mates =
                                                Session.CurrentMapInstance.GetListMateInRange(Session.Character.PositionX,
                                                        Session.Character.PositionY, ski.TargetRange());
                                        if (mates != null)
                                        {
                                            foreach (var target in mates)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity)
                                                )
                                                {
                                                    if (Session.Character.MapInstance ==
                                                        ServerManager.Instance.ArenaInstance &&
                                                        (Session.Character.Group == null ||
                                                         !Session.Character.Group.IsMemberOfGroup(target.Owner
                                                                                                        .CharacterId)))
                                                    {
                                                        continue;
                                                    }

                                                    if (Session.Character.MapInstance ==
                                                        ServerManager.Instance.FamilyArenaInstance &&
                                                        Session.Character.Family != target.Owner.Family)
                                                    {
                                                        continue;
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s =>
                                                               !s.Type.Equals((byte)BCardType.CardType.MeditationSkill))
                                                       .ToList().ForEach(s =>
                                                               s.ApplyBCards(target.BattleEntity,
                                                                       Session.Character.BattleEntity,
                                                                       partnerBuffLevel: ski.TattooLevel));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(
                                                            UserType.Player,
                                                            Session.Character.CharacterId,
                                                            (byte)target.BattleEntity.UserType, target.MateTransportId,
                                                            ski.Skill.SkillVNum,
                                                            (short)(ski.Skill.Cooldown),
                                                            ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                            target.PositionX, target.PositionY, true,
                                                            (int)(target.Hp / target.HpLoad() * 100), 0, -1,
                                                            (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<MapMonster> monsters =
                                                Session.CurrentMapInstance.GetMonsterInRangeList(
                                                        Session.Character.PositionX, Session.Character.PositionY,
                                                        ski.TargetRange());
                                        if (monsters != null)
                                        {
                                            foreach (var target in monsters)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity)
                                                )
                                                {
                                                    if (target.Owner != null)
                                                    {
                                                        if (target.Owner.Character != null)
                                                        {
                                                            continue;
                                                        }

                                                        if (Session.Character.MapInstance ==
                                                            ServerManager.Instance.ArenaInstance &&
                                                            (Session.Character.Group == null ||
                                                             !Session.Character.Group.IsMemberOfGroup(target.Owner
                                                                                                            .MapEntityId)))
                                                        {
                                                            continue;
                                                        }

                                                        if (Session.Character.MapInstance ==
                                                            ServerManager.Instance.FamilyArenaInstance &&
                                                            Session.Character.Family != target.Owner.Character?.Family)
                                                        {
                                                            continue;
                                                        }
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s =>
                                                               !s.Type.Equals((byte)BCardType.CardType.MeditationSkill))
                                                       .ToList().ForEach(s =>
                                                               s.ApplyBCards(target.BattleEntity,
                                                                       Session.Character.BattleEntity,
                                                                       partnerBuffLevel: ski.TattooLevel));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(
                                                            UserType.Player,
                                                            Session.Character.CharacterId,
                                                            (byte)target.BattleEntity.UserType, target.MapMonsterId,
                                                            ski.Skill.SkillVNum,
                                                            (short)(ski.Skill.Cooldown),
                                                            ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                            target.MapX, target.MapY, true,
                                                            (int)(target.CurrentHp / target.MaxHp * 100), 0, -1,
                                                            (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        IEnumerable<MapNpc> npcs =
                                                Session.CurrentMapInstance.GetListNpcInRange(Session.Character.PositionX,
                                                        Session.Character.PositionY, ski.TargetRange());
                                        if (npcs != null)
                                        {
                                            foreach (var target in npcs)
                                            {
                                                if (!Session.Character.BattleEntity.CanAttackEntity(target.BattleEntity)
                                                )
                                                {
                                                    if (target.Owner != null)
                                                    {
                                                        //if (Session.Character.MapInstance ==
                                                        //    ServerManager.Instance.ArenaInstance &&
                                                        //    (Session.Character.Group == null ||
                                                        //     !Session.Character.Group.IsMemberOfGroup(target.Owner
                                                        //                                                    .MapEntityId)))
                                                        //{
                                                        //    continue;
                                                        //}

                                                        //if (Session.Character.MapInstance ==
                                                        //    ServerManager.Instance.FamilyArenaInstance &&
                                                        //    Session.Character.Family != target.Owner.Character?.Family)
                                                        //{
                                                        //    continue;
                                                        //}
                                                    }

                                                    ski.GetSkillBCards().ToList().Where(s =>
                                                               !s.Type.Equals((byte)BCardType.CardType.MeditationSkill))
                                                       .ToList().ForEach(s =>
                                                               s.ApplyBCards(target.BattleEntity,
                                                                       Session.Character.BattleEntity,
                                                                       partnerBuffLevel: ski.TattooLevel));

                                                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(
                                                            UserType.Player,
                                                            Session.Character.CharacterId,
                                                            (byte)target.BattleEntity.UserType, target.MapNpcId,
                                                            ski.Skill.SkillVNum,
                                                            (short)(ski.Skill.Cooldown),
                                                            ski.Skill.AttackAnimation, ski.Skill.Effect,
                                                            target.MapX, target.MapY, true,
                                                            (int)(target.CurrentHp / target.MaxHp * 100), 0, -1,
                                                            (byte)(ski.Skill.SkillType - 1)));
                                                }
                                            }
                                        }

                                        break;
                                }
                            }

                            ski.GetSkillBCards().ToList()
                               .Where(s => !s.Type.Equals((byte)BCardType.CardType.MeditationSkill)).ToList()
                               .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity,
                                       Session.Character.BattleEntity, partnerBuffLevel: ski.TattooLevel));
                        }
                        else if (ski.Skill.TargetType == 0)
                        {
                            if (Session.Character.MapInstance.MapInstanceType ==
                                MapInstanceType.TalentArenaMapInstance && !Session.Character.MapInstance.IsPVP)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                return;
                            }

                            if (isPvp)
                            {
                                //ClientSession playerToAttack = ServerManager.Instance.GetSessionByCharacterId(targetId);
                                var playerToAttack = targetEntity.Character?.Session;

                                if (playerToAttack != null && !IceBreaker.FrozenPlayers.Contains(playerToAttack))
                                {
                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell
                                            {
                                                X = playerToAttack.Character.PositionX,
                                                Y = playerToAttack.Character.PositionY
                                            }) <= ski.Skill.Range + 5)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 1 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast(
                                                $"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        Session.SendPacket(Session.Character.GenerateStat());
                                        var characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);
                                        Session.CurrentMapInstance.Broadcast(
                                            StaticPacketHelper.CastOnTarget(UserType.Player,
                                                Session.Character.CharacterId, UserType.Player, targetId,
                                                ski.Skill.CastAnimation,
                                                characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                                ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;

                                        // We will check if there's a cooldown reduction in queue
                                        if (cooldownReduction != 0)
                                        {
                                            ski.LastUse = ski.LastUse.AddMilliseconds((reducedCooldown) * -1 * 100);
                                        }

                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            var count = 0;
                                            if (playerToAttack.CurrentMapInstance == Session.CurrentMapInstance
                                                && playerToAttack.Character.CharacterId !=
                                                Session.Character.CharacterId)
                                            {
                                                if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack
                                                    .Character.BattleEntity))
                                                {
                                                    count++;
                                                    Session.PvpHit(
                                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill, skillBCards: ski.GetSkillBCards(),
                                                            showTargetAnimation: true), playerToAttack);
                                                }
                                                else
                                                {
                                                    Session.SendPacket(
                                                        StaticPacketHelper.Cancel(2, targetId));
                                                }
                                            }

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Player).Select(s => s.TargetId))
                                            foreach (var id in Session.Character.GetMTListTargetQueue_QuickFix(ski,
                                                UserType.Player))
                                            {
                                                var character = ServerManager.Instance.GetSessionByCharacterId(id);

                                                if (character != null
                                                    && character.CurrentMapInstance == Session.CurrentMapInstance
                                                    && character.Character.CharacterId != Session.Character.CharacterId
                                                    && character != playerToAttack)
                                                {
                                                    if (Session.Character.BattleEntity.CanAttackEntity(
                                                            character.Character.BattleEntity))
                                                    {
                                                        count++;
                                                        Session.PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                                        ski.Skill, showTargetAnimation: count == 1,
                                                                        skillBCards: ski.GetSkillBCards()), character);
                                                    }
                                                }
                                            }

                                            if (count == 0)
                                            {
                                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                            }
                                        }
                                        else
                                        {
                                            // check if we will hit mutltiple targets
                                            if (ski.TargetRange() != 0)
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    var playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s != playerToAttack
                                                            && s.Character.IsInRange(playerToAttack.Character.PositionX,
                                                                playerToAttack.Character.PositionY, ski.TargetRange()));
                                                    var count = 0;
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack
                                                        .Character.BattleEntity))
                                                    {
                                                        count++;
                                                        Session.PvpHit(
                                                            new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                Session, ski.Skill, skillCombo: skillCombo,
                                                                skillBCards: ski.GetSkillBCards()),
                                                            playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    foreach (var character in playersInAoeRange)
                                                    {
                                                        if (Session.Character.BattleEntity.CanAttackEntity(
                                                                character.Character.BattleEntity))
                                                        {
                                                            count++;
                                                            Session.PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                            Session, ski.Skill, skillCombo: skillCombo,
                                                                            showTargetAnimation: count == 1,
                                                                            skillBCards: ski.GetSkillBCards()),
                                                                    character);
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0 || count == 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    var playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s != playerToAttack
                                                            && s.Character.IsInRange(playerToAttack.Character.PositionX,
                                                                playerToAttack.Character.PositionY, ski.TargetRange()));

                                                    var count = 0;

                                                    // hit the targetted player
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack
                                                        .Character.BattleEntity))
                                                    {
                                                        count++;
                                                        Session.PvpHit(
                                                            new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                Session, ski.Skill, showTargetAnimation: true,
                                                                skillBCards: ski.GetSkillBCards()), playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                            StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    //hit all other players
                                                    foreach (var character in playersInAoeRange)
                                                    {
                                                        count++;
                                                        if (Session.Character.BattleEntity.CanAttackEntity(
                                                            character.Character.BattleEntity))
                                                        {
                                                            Session.PvpHit(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                            Session, ski.Skill, showTargetAnimation: count == 1,
                                                                            skillBCards: ski.GetSkillBCards()), character);
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack
                                                                                                       .Character.BattleEntity))
                                                    {
                                                        Session.PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                        Session, ski.Skill, skillCombo: skillCombo,
                                                                        skillBCards: ski.GetSkillBCards()),
                                                                playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                                StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    if (Session.Character.BattleEntity.CanAttackEntity(playerToAttack
                                                        .Character.BattleEntity))
                                                    {
                                                        Session.PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHit,
                                                                        Session, ski.Skill, showTargetAnimation: true,
                                                                        skillBCards: ski.GetSkillBCards()), playerToAttack);
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(
                                                                StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                        return;
                                    }
                                }
                                else if (IceBreaker.FrozenPlayers.Contains(playerToAttack))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    if (playerToAttack.Character.LastPvPKiller == null
                                        || playerToAttack.Character.LastPvPKiller != Session)
                                    {
                                        Session.SendPacket(
                                                $"delay 2000 5 #guri^502^1^{playerToAttack.Character.CharacterId}");
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    return;
                                }
                            }
                            else
                            {
                                var monsterToAttack = targetEntity.MapMonster;

                                if (monsterToAttack != null)
                                {
                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell { X = monsterToAttack.MapX, Y = monsterToAttack.MapY }) <=
                                        ski.Skill.Range + 5 + monsterToAttack.Monster.BasicArea)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        #region Taunt

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 3 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast(
                                                $"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        #endregion

                                        ski.GetSkillBCards().ToList().Where(s => s.CastType == 1).ToList()
                                            .ForEach(s => s.ApplyBCards(monsterToAttack.BattleEntity,
                                                Session.Character.BattleEntity, partnerBuffLevel: ski.TattooLevel));

                                        Session.SendPacket(Session.Character.GenerateStat());

                                        var ski2 = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum
                                            && s.Skill.Effect > 0 && s.Skill.SkillType == 2);

                                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(
                                            UserType.Player, Session.Character.CharacterId, UserType.Monster,
                                            monsterToAttack.MapMonsterId,
                                            ski.Skill.CastAnimation, ski2?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                            ski.Skill.SkillVNum));

                                        Session.Character.Skills.Where(x => x.Id != ski.Id).ForEach(x => x.Hit = 0);

                                        #region Generate scp

                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        #endregion

                                        ski.LastUse = DateTime.Now;

                                        // We will check if there's a cooldown reduction in queue
                                        if (cooldownReduction != 0)
                                        {
                                            ski.LastUse = ski.LastUse.AddMilliseconds((reducedCooldown) * -1 * 100);
                                        }

                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            monsterToAttack.HitQueue.Enqueue(new HitRequest(
                                                TargetHitType.SingleAOETargetHit, Session,
                                                ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect,
                                                showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                                            foreach (var id in Session.Character.GetMTListTargetQueue_QuickFix(ski,
                                                UserType.Monster))
                                            {
                                                var mon = Session.CurrentMapInstance.GetMonsterById(id);

                                                if (mon?.CurrentHp > 0)
                                                {
                                                    mon.HitQueue.Enqueue(new HitRequest(
                                                            TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect,
                                                            skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ski.TargetRange() != 0 || ski.Skill.HitType == 1)
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);

                                                var monstersInAoeRange = Session.CurrentMapInstance
                                                    ?.GetMonsterInRangeList(monsterToAttack.MapX, monsterToAttack.MapY,
                                                        ski.TargetRange())?
                                                    .Where(m =>
                                                        Session.Character.BattleEntity.CanAttackEntity(m.BattleEntity))
                                                    .ToList();

                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit ==
                                                        ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    if (monsterToAttack.IsAlive && monstersInAoeRange?.Count != 0)
                                                    {
                                                        foreach (var mon in monstersInAoeRange)
                                                        {
                                                            mon.HitQueue.Enqueue(new HitRequest(
                                                                    TargetHitType.SingleTargetHitCombo, Session,
                                                                    ski.Skill, skillCombo: skillCombo,
                                                                    skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(
                                                        TargetHitType.SingleAOETargetHit, Session,
                                                        ski.Skill, ski2?.Skill.Effect ?? ski.Skill.Effect,
                                                        showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                                    if (monsterToAttack.IsAlive && monstersInAoeRange?.Count != 0)
                                                    {
                                                        foreach (var mon in monstersInAoeRange.Where(m =>
                                                                m.MapMonsterId != monsterToAttack.MapMonsterId))
                                                        {
                                                            mon.HitQueue.Enqueue(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                            Session, ski.Skill,
                                                                            ski2?.Skill.Effect ?? ski.Skill.Effect,
                                                                            skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);

                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit ==
                                                        ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(
                                                                            TargetHitType.SingleTargetHitCombo, Session,
                                                                            ski.Skill, skillCombo: skillCombo,
                                                                            skillBCards: ski.GetSkillBCards()));
                                                }
                                                else
                                                {
                                                    monsterToAttack.HitQueue.Enqueue(new HitRequest(
                                                        TargetHitType.SingleTargetHit, Session,
                                                        ski.Skill, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                        return;
                                    }
                                }
                                else if (targetEntity.Mate is Mate mateToAttack)
                                {
                                    if (!Session.Character.BattleEntity.CanAttackEntity(mateToAttack.BattleEntity))
                                    {
                                        Session.Character.Session.SendPacket(
                                            StaticPacketHelper.Cancel(2, mateToAttack.BattleEntity.MapEntityId));
                                        return;
                                    }

                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell { X = mateToAttack.PositionX, Y = mateToAttack.PositionY })
                                        <= ski.Skill.Range + 5 + mateToAttack.Monster.BasicArea)
                                    {
                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        if (ski.SkillVNum == 1061)
                                        {
                                            Session.CurrentMapInstance.Broadcast($"eff 2 {targetId} 4968");
                                            Session.CurrentMapInstance.Broadcast(
                                                $"eff 1 {Session.Character.CharacterId} 4968");
                                        }

                                        ski.GetSkillBCards().ToList().Where(s => s.CastType == 1).ToList().ForEach(s =>
                                            s.ApplyBCards(mateToAttack.BattleEntity, Session.Character.BattleEntity,
                                                partnerBuffLevel: ski.TattooLevel));

                                        Session.SendPacket(Session.Character.GenerateStat());
                                        var characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);

                                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(
                                            UserType.Player, Session.Character.CharacterId, UserType.Npc,
                                            mateToAttack.MateTransportId, ski.Skill.CastAnimation,
                                            characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                            ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;

                                        // We will check if there's a cooldown reduction in queue
                                        if (cooldownReduction != 0)
                                        {
                                            ski.LastUse = ski.LastUse.AddMilliseconds((reducedCooldown) * -1 * 100);
                                        }


                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            mateToAttack.HitRequest(new HitRequest(
                                                TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                showTargetAnimation: true, skillBCards: ski.GetSkillBCards()));

                                            //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                                            foreach (var id in Session.Character.GetMTListTargetQueue_QuickFix(ski,
                                                UserType.Monster))
                                            {
                                                var mate = Session.CurrentMapInstance.GetMate(id);
                                                if (mate != null && mate.Hp > 0 &&
                                                    Session.Character.BattleEntity.CanAttackEntity(mate.BattleEntity))
                                                {
                                                    mate.HitRequest(new HitRequest(
                                                            TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                            characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                            skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ski.TargetRange() != 0 || ski.Skill.HitType == 1
                                            ) // check if we will hit mutltiple targets
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    var monstersInAoeRange = Session.CurrentMapInstance?
                                                                                     .GetListMateInRange(mateToAttack.MapX,
                                                                                             mateToAttack.MapY, ski.TargetRange()).Where(m =>
                                                                                             Session.Character.BattleEntity.CanAttackEntity(
                                                                                                             m.BattleEntity)).ToList();
                                                    if (monstersInAoeRange.Count != 0)
                                                    {
                                                        foreach (var mate in monstersInAoeRange)
                                                        {
                                                            mate.HitRequest(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                            Session, ski.Skill, skillCombo: skillCombo,
                                                                            skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!mateToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    var matesInAoeRange = Session.CurrentMapInstance?
                                                        .GetListMateInRange(
                                                            mateToAttack.MapX,
                                                            mateToAttack.MapY,
                                                            ski.TargetRange())
                                                        ?.Where(m =>
                                                            Session.Character.BattleEntity.CanAttackEntity(
                                                                m.BattleEntity)).ToList();

                                                    //hit the targetted mate
                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill,
                                                            characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                            showTargetAnimation: true,
                                                            skillBCards: ski.GetSkillBCards()));

                                                    //hit all other mates
                                                    if (matesInAoeRange != null && matesInAoeRange.Count != 0)
                                                    {
                                                        foreach (var mate in matesInAoeRange.Where(m =>
                                                                m.MateTransportId != mateToAttack.MateTransportId)
                                                        ) //exclude targetted mates
                                                        {
                                                            mate.HitRequest(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                            Session, ski.Skill,
                                                                            characterSkillInfo?.Skill.Effect ??
                                                                            ski.Skill.Effect,
                                                                            skillBCards: ski.GetSkillBCards()));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!mateToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleTargetHitCombo, Session,
                                                            ski.Skill, skillCombo: skillCombo,
                                                            skillBCards: ski.GetSkillBCards()));
                                                }
                                                else
                                                {
                                                    mateToAttack.HitRequest(
                                                        new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                            ski.Skill, skillBCards: ski.GetSkillBCards()));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                        return;
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                }
                            }

                            if (ski.Skill.HitType == 3)
                            {
                                Session.Character.MTListTargetQueue.Clear();
                            }

                            ski.GetSkillBCards().Where(s =>
                                       s.Type.Equals((byte)BCardType.CardType.Buff) &&
                                       new Buff((short)s.SecondData, Session.Character.Level).Card?.BuffType ==
                                       BuffType.Good).ToList()
                               .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity,
                                       Session.Character.BattleEntity, partnerBuffLevel: ski.TattooLevel));
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        }

                        if (ski.Skill.SkillVNum != 1098 && ski.Skill.SkillVNum != 1330)
                        {
                            Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(castingId,
                                    (short)(ski.Skill.Cooldown)));
                        }

                        var cdResetMilliseconds =
                            (int)((ski.Skill.Cooldown)) * 100;
                        Observable.Timer(TimeSpan.FromMilliseconds(cdResetMilliseconds))
                            .Subscribe(o =>
                            {
                                sendSkillReset();
                                if (cdResetMilliseconds <= 500)
                                {
                                    Observable.Timer(TimeSpan.FromMilliseconds(500)).Subscribe(obs => sendSkillReset());
                                }

                                void sendSkillReset()
                                {
                                    var charSkills = Session.Character.GetSkills();

                                    var skill = charSkills.Find(s =>
                                        s.Skill?.CastId == castingId &&
                                        (s.Skill?.UpgradeSkill == 0 ||
                                         s.Skill?.SkillType == (byte)SkillType.CharacterSKill));

                                    var dateTimeNow = DateTime.Now;
                                    if (skill != null)
                                    {
                                        if (cooldownReduction < 0)
                                        {

                                        }

                                        Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                                        skill.ReinstantiateSkill();
                                    }
                                }
                            });

                        // This will reset skill's cooldown if you have fairy wings
                        int[] fairyWings = Session.Character.GetBuff(CardType.EffectSummon, 11);
                        int random = ServerManager.RandomNumber();
                        if (fairyWings[0] > random)
                        {
                            Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                            {
                                if (ski != null)
                                {
                                    ski.LastUse = DateTime.Now.AddMilliseconds(ski.Skill.Cooldown * 100 * -1);
                                    Session.SendPacket(StaticPacketHelper.SkillReset(ski.Skill.CastId));
                                }
                            });
                        }
                    }
                    else
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    }
                }

            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
            }

            if (castingId != 0 && castingId < 11 && shouldCancel || Session.Character.SkillComboCount > 7)
            {
                Session.SendPackets(Session.Character.GenerateQuicklist());

                if (!Session.Character.HasMagicSpellCombo
                    && Session.Character.SkillComboCount > 7)
                {
                    CharacterExtension.GenerateMSlot(Session);
                }
            }

            Session.Character.LastSkillUse = DateTime.Now;
        }

        public static void ZoneHit(this ClientSession Session, int castingId, short x, short y)
        {
            var characterSkill = Session.Character.GetSkills()?.Find(s => s.Skill?.CastId == castingId);
            if (characterSkill == null || !Session.Character.WeaponLoaded(characterSkill)
                                       || !Session.HasCurrentMapInstance
                                       || (x != 0 || y != 0) &&
                                       !Session.Character.IsInRange(x, y, characterSkill.GetSkillRange() + 1))
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
                return;
            }

            if (characterSkill?.Skill == null)
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
                return;
            }

            if (characterSkill.CanBeUsed())
            {
                var mpCost = characterSkill.MpCost();
                short hpCost = 0;

                mpCost = (short)(mpCost * ((100 - Session.Character.CellonOptions
                                                 .Where(s => s.Type == CellonOptionType.MPUsage).Sum(s => s.Value)) /
                                            100D));

                if (Session.Character.GetBuff(CardType.HealingBurningAndCasting,
                        (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP)[0] is int
                    HPDecreasedByConsumingMP)
                {
                    if (HPDecreasedByConsumingMP < 0)
                    {
                        var amountDecreased = characterSkill.MpCost() * HPDecreasedByConsumingMP / 100;
                        hpCost = (short)amountDecreased;
                        mpCost -= (short)amountDecreased;
                    }
                }

                if (Session.Character.Mp >= mpCost && Session.Character.Hp > hpCost && Session.HasCurrentMapInstance)
                {
                    Session.Character.LastSkillUse = DateTime.Now;

                    double cooldownReduction =
                        Session.Character.GetBuff(CardType.Morale,
                            (byte)AdditionalTypes.Morale.SkillCooldownDecreased)[0] +
                        Session.Character.GetBuff(CardType.Casting,
                            (byte)AdditionalTypes.Casting.EffectDurationIncreased)[0];

                    var mCd = new BattleEntity(Session.Character, characterSkill.Skill);

                    if (mCd.AttackType == AttackType.Magical)
                    {
                        cooldownReduction += Session.Character.GetBuff(BCardType.CardType.AbsorbedSpirit,
                                                   (byte)AdditionalTypes.AbsorbedSpirit.MagicCooldownDecreased)[0];
                    }

                    var increaseEnemyCooldownChance = Session.Character.GetBuff(CardType.DarkCloneSummon,
                        (byte)AdditionalTypes.DarkCloneSummon.IncreaseEnemyCooldownChance);

                    if (ServerManager.RandomNumber() < increaseEnemyCooldownChance[0])
                    {
                        cooldownReduction -= increaseEnemyCooldownChance[1];
                    }

                    Session.CurrentMapInstance.Broadcast(
                                    $"ct_n 1 {Session.Character.CharacterId} 3 -1 {characterSkill.Skill.CastAnimation}" +
                                    $" {characterSkill.Skill.CastEffect} {characterSkill.Skill.SkillVNum}");

                    characterSkill.LastUse = DateTime.Now;

                    // We save the reduced cooldown amount for using it later
                    var reducedCooldown = (characterSkill.Skill.Cooldown * (cooldownReduction / 100D));

                    // We will check if there's a cooldown reduction in queue
                    if (cooldownReduction != 0)
                    {
                        characterSkill.LastUse = characterSkill.LastUse.AddMilliseconds((reducedCooldown) * -1 * 100);
                    }

                    if (Session?.Character?.HasGodMode == false)
                    {
                        Session.Character.DecreaseMp(characterSkill.MpCost());
                    }

                    Observable.Timer(TimeSpan.FromMilliseconds(characterSkill.Skill.CastTime * 100)).Subscribe(o =>
                    {
                        Session.CurrentMapInstance.Broadcast(
                            $"bs 1 {Session.Character.CharacterId} {x} {y} {characterSkill.Skill.SkillVNum}" +
                            $" {(short)(characterSkill.Skill.Cooldown - reducedCooldown)} {characterSkill.Skill.AttackAnimation}" +
                            $" {characterSkill.Skill.Effect} 0 0 1 1 0 0 0");

                        var Range = characterSkill.TargetRange();
                        if (characterSkill.GetSkillBCards().Any(s =>
                            s.Type == (byte)BCardType.CardType.FalconSkill &&
                            s.SubType == (byte)AdditionalTypes.FalconSkill.FalconFocusLowestHP))
                        {
                            if (Session.CurrentMapInstance.BattleEntities.Where(s => s.IsInRange(x, y, Range)
                                                                                     && Session.Character.BattleEntity
                                                                                         .CanAttackEntity(s))
                                .OrderBy(s => s.Hp).FirstOrDefault() is BattleEntity lowestHPEntity)
                            {
                                Session.Character.MTListTargetQueue.Push(new MTListHitTarget(lowestHPEntity.UserType,
                                        lowestHPEntity.MapEntityId, (TargetHitType)characterSkill.Skill.HitType));
                            }
                        }
                        else if (Session.Character.MTListTargetQueue.Count == 0)
                        {
                            Session.CurrentMapInstance.BattleEntities
                                .Where(s => s.IsInRange(x, y, Range) &&
                                            Session.Character.BattleEntity.CanAttackEntity(s))
                                .ToList().ForEach(s =>
                                    Session.Character.MTListTargetQueue.Push(new MTListHitTarget(s.UserType,
                                        s.MapEntityId, (TargetHitType)characterSkill.Skill.HitType)));
                        }

                        var count = 0;

                        //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                        foreach (var id in Session.Character.GetMTListTargetQueue_QuickFix(characterSkill,
                    UserType.Monster))
                        {
                            var mon = Session.CurrentMapInstance.GetMonsterById(id);
                            if (mon?.CurrentHp > 0 && mon?.Owner?.MapEntityId != Session.Character.CharacterId)
                            {
                                count++;
                                mon.HitQueue.Enqueue(new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                    characterSkill.Skill, characterSkill.Skill.Effect, x, y,
                                    showTargetAnimation: count == 0, skillBCards: characterSkill.GetSkillBCards()));
                            }
                        }

                        //foreach (long id in Session.Character.MTListTargetQueue.Where(s => s.EntityType == UserType.Player).Select(s => s.TargetId))
                        foreach (var id in Session.Character.GetMTListTargetQueue_QuickFix(characterSkill,
                    UserType.Player))
                        {
                            var character = ServerManager.Instance.GetSessionByCharacterId(id);
                            if (character != null && character?.CurrentMapInstance == Session?.CurrentMapInstance
                                                  && character?.Character.CharacterId != Session?.Character.CharacterId)
                            {
                                if (Session.Character.BattleEntity.CanAttackEntity(character?.Character?.BattleEntity))
                                {
                                    count++;
                                    Session?.PvpHit(
                                            new HitRequest(TargetHitType.SingleAOETargetHit, Session, characterSkill.Skill,
                                                    characterSkill.Skill.Effect, x, y, showTargetAnimation: count == 0,
                                                    skillBCards: characterSkill.GetSkillBCards()),
                                            character);
                                }
                            }
                        }

                        characterSkill.GetSkillBCards().ToList().Where(s =>
                                s.Type.Equals((byte)BCardType.CardType.Buff) &&
                                new Buff((short)s.SecondData, Session.Character.Level).Card.BuffType.Equals(
                                    BuffType.Good)
                                || s.Type.Equals((byte)BCardType.CardType.FalconSkill) &&
                                s.SubType.Equals((byte)AdditionalTypes.FalconSkill.CausingChanceLocation)
                                || s.Type.Equals((byte)BCardType.CardType.FearSkill) &&
                                s.SubType.Equals((byte)AdditionalTypes.FearSkill.ProduceWhenAmbushe)).ToList()
                            .ForEach(s => s.ApplyBCards(Session.Character.BattleEntity, Session.Character.BattleEntity,
                                x, y, characterSkill.TattooLevel));

                        Session.Character.MTListTargetQueue.Clear();
                    });

                    Observable.Timer(TimeSpan.FromMilliseconds(
                            (short)(characterSkill.Skill.Cooldown - reducedCooldown) * 100))
                        .Subscribe(o =>
                        {
                            var
                                skill = Session.Character.GetSkills().Find(s =>
                                    s.Skill?.CastId
                                    == castingId &&
                                    (s.Skill?.UpgradeSkill == 0 ||
                                     s.Skill?.SkillType == (byte)SkillType.CharacterSKill));
                            if (skill != null &&
                                skill.LastUse.AddMilliseconds(
                                    (short)(characterSkill.Skill.Cooldown - reducedCooldown) * 100 - 100) <=
                                DateTime.Now)
                            {

                                Session.SendPacket(StaticPacketHelper.SkillReset(castingId));
                            }
                        });
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    Session.SendPacket(StaticPacketHelper.Cancel(2));
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
            }
        }

        #endregion
    }
}