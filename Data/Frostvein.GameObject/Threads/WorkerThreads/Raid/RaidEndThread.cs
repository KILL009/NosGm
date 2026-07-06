using Frostvein.Configuration;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System.Reactive.Linq;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Frostvein.Master.Library.Data;

namespace Frostvein.GameObject.Raid.Threads
{
    public static class RaidEndThread
    {

        public static async Task Run(EventContainer evt, ClientSession session = null, MapMonster monster = null, MapNpc npc = null)
        {
            evt.MapInstance.InstanceBag.EndState = (byte)evt.Parameter;
            var owner = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character.Group?.Raid?.InstanceBag.CreatorId == s.Character.CharacterId)?.Character;

            if (owner == null)
            {
                owner = evt.MapInstance.Sessions.FirstOrDefault(s => s.Character.Group?.Raid != null)?.Character;

            }

            var group = owner?.Group;

            if (group?.Raid == null)
            {
                return;
            }

            var teamSize = group.Raid.InstanceBag.Lives;

            if (evt.MapInstance.InstanceBag.EndState == 1 && evt.MapInstance.Monsters.Any(s => s.IsBoss))
            {
                foreach (var s in group.Sessions.Where(s => s?.Character?.MapInstance?.Monsters.Any(e => e.IsBoss) == true))

                {
                    s.SendPacket(StaticPacketHelper.Cancel(2, s.Character.CharacterId));

                    foreach (var gift in group.Raid.GiftItems)
                    {

                        sbyte rare;
                        if (gift.IsHeroic)
                        {
                            rare = (sbyte)(gift.IsRandomRare ? ServerManager.RandomNumber(0, 9) : 0);
                        }
                        else
                        {
                            rare = (sbyte)(gift.IsRandomRare ? ServerManager.RandomNumber(0, 8) : 0);
                        }

                        if (s.Character.Level >= group.Raid.LevelMinimum)
                        {
                            if (gift.MinTeamSize == 0 && gift.MaxTeamSize == 0 || teamSize >= gift.MinTeamSize && teamSize <= gift.MaxTeamSize)
                            {
                                if (EventConfiguration.IsActivated && EventConfiguration.DoubleBox > 0)
                                {
                                    int rnd = ServerManager.RandomNumber(0, 100);
                                    if (rnd <= EventConfiguration.DoubleBox)
                                    {
                                        s.Character.GiftAdd(gift.VNum, gift.Amount, (byte)rare, 0, gift.Design, gift.IsRandomRare);
                                        MessageExtension.SendGreen(s, "Congratulations! You received a second Raidbox!");
                                    }
                                }
                                s.Character.GiftAdd(gift.VNum, gift.Amount, (byte)rare, 0, gift.Design, gift.IsRandomRare);
                            }
                        }
                    }

                    try { MessageExtension.SendRaidDamage(s, 1, evt); }
                    catch { MessageExtension.SendGreen(s, "Something went wrong while updating the Raid Damage"); }

                    try { MessageExtension.SendRaidDamage(s, 2, evt); }
                    catch { MessageExtension.SendGreen(s, "Something went wrong while updating the Raid Damage for all Players"); }

                    s.Character.GetReputation(group.Raid.Reputation);
                    MessageExtension.SendGreen(s, $"Your Reputation increased by {group.Raid.Reputation}");

                    if (group.Raid.FamExp != null || group.Raid.FamExp > 0)
                    {
                        MessageExtension.SendGreen(s, $"Your Family EXP increased by {group.Raid.FamExp}");
                    }
                    s.Character.DamageInRaid = 0;
                    s.Character.RaidCount += 1;
                    InstanceExtension.AddBattlePassPoint(s);
                    //await ItemThread.ItemThread.Add(s, 13008, 1);
                    if (s.Character.PrimalRaidQuest != 0)
                    {
                        PrimalQuestRewardExtension.GenerateRaidReward(s, s.Character.PrimalRaidQuest);
                    }

                    if (s.Character.GenerateFamilyXp(group.Raid.FamExp, group.Raid.Id))
                    {
                        MessageExtension.SendGreen(session, $"You won {group.Raid.FamExp} Family EXP");
                    }

                    s.Character.IncrementQuests(QuestType.WinRaid, group.Raid.Id);

                    if (s.Character.HasBuff(5002))
                    {
                        s.Character.RemoveBuff(5002);
                    }
                }

                //FAMILY MISSIONS IN RAID
                foreach (var Family in FamilyExtensions.SessionsToFamilies(group.Sessions.Where(s => s?.Character?.MapInstance?.Monsters.Any(e => e.IsBoss) ?? false)))
                {
                    foreach (var fsm in group.Raid.FamMissions)
                    {
                        if (fsm == 0) continue;
                        Family.AddMissionProgress((short)fsm, (short)(group.Sessions.Where(s => (s?.Character?.MapInstance?.Monsters.Any(e => e.IsBoss) ?? false) && (s?.Character?.Family?.FamilyId.Equals(Family.FamilyId) ?? false)).Count()));
                    }
                }


                foreach (var mapMonster in evt.MapInstance.Monsters)
                {
                    if (mapMonster != null)
                    {
                        mapMonster.SetDeathStatement();
                        evt.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                                mapMonster.MapMonsterId));
                        evt.MapInstance.RemoveMonster(mapMonster);
                    }
                }

                ServerManager.Instance.Broadcast($"msg 5 [Team {owner.Name}] successfully completed the {group.Raid.Label} Raid", ReceiverType.All);
            }

            var dueTime = TimeSpan.FromSeconds(evt.MapInstance.InstanceBag.EndState == 1 ? 15 : 0);

            evt.MapInstance.Broadcast(Character.GenerateRaidBf(evt.MapInstance.InstanceBag.EndState));

            Observable.Timer(dueTime).Subscribe(async o =>
            {
                evt.MapInstance.Sessions.Where(s =>
                           s.Character != null &&
                           s.Character.HasBuff(BCardType.CardType.FrozenDebuff,
                                   (byte)AdditionalTypes.FrozenDebuff.EternalIce))
                   .Select(s => s.Character).ToList().ForEach(c => { c.RemoveBuff(569); });

                var groupMembers = new ClientSession[group.SessionCount];
                group.Sessions.CopyTo(groupMembers);

                foreach (var groupMember in groupMembers)
                {
                    if (groupMember.Character.Hp < 1)
                    {
                        groupMember.Character.Hp = 1;
                        groupMember.Character.Mp = 1;
                    }

                    groupMember.SendPacket(groupMember.Character.GenerateRaid(1, true));
                    groupMember.SendPacket(groupMember.Character.GenerateRaid(2, true));
                    group.LeaveGroup(groupMember);
                }

                ServerManager.Instance.GroupList.RemoveAll(s => s.GroupId == group.GroupId);
                ServerManager.Instance.ThreadSafeGroupList.Remove(group.GroupId);

                group.Raid.Dispose();
                await group.Raid.DisposeAsync();
            });

        }
    }
}
