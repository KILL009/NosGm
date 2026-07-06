using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Frostvein.GameObject
{
    public class SnackItem : Item
    {
        #region Instantiation

        public SnackItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Properties

        private static IDisposable _regenerateDisposable { get; set; }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            session.Character.SnackRequests++;
            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
            {
                session.SendPacket("msg 4 You cannot do that here");
                return;
            }
            if (session.Character.LastDelayRecovery > DateTime.Now)
            {
                return;
            }

            if (session.CurrentMapInstance?.MapInstanceType != MapInstanceType.TalentArenaMapInstance && VNum == 2802)
            {
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && VNum != 2802)
            {
                return;
            }

            if (session.Character.MapId == 2010 || session.Character.MapId == 2005 || session.Character.MapId == 2015)
            {
                session.SendPacket(session.Character.GenerateSay("Can't use Snacks in this Map.", 11));
                return;
            }

            var item = inv.Item;
            switch (Effect)
            {
                default:
                    if (session.Character.Hp <= 0)
                    {
                        return;
                    }

                    if (item.BCards.Find(s => s.Type == 25) is BCard Buff)
                    {
                        if (ServerManager.RandomNumber() < Buff.FirstData)
                        {
                            session.Character.AddBuff(new Buff((short)Buff.SecondData, session.Character.Level),
                                    session.Character.BattleEntity);
                        }

                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else
                    {
                        if (session.Character.SnackAmount < 0)
                        {
                            session.Character.SnackAmount = 0;
                        }

                        var amount = session.Character.SnackAmount;
                        if (amount < 5)
                        {
                            var workerThread = new Thread(() => Regenerate(session, item));
                            workerThread.Start();
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(session.Character.Gender == GenderType.Female
                                ? session.Character.GenerateSay(
                                    Language.Instance.GetMessageFromKey("NOT_HUNGRY_FEMALE"), 1)
                                : session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_HUNGRY_MALE"),
                                    1));
                        }

                        if (amount == 0)
                        {
                            var workerThread2 = new Thread(() => Sync(session));
                            workerThread2.Start();
                        }
                    }

                    break;
            }

            if (session.Character.SnackRequests > 50)
            {
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = session.Character.AccountId,
                    Reason = "Auto Warning Snack Abuse PL",
                    Penalty = PenaltyType.IPBanned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(20),
                    AdminName = "Frostvein"
                };
                Character.InsertOrUpdatePenalty(log);
                session?.Disconnect();
                return;
            }
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(x =>
            {
                if (session?.Character?.SnackRequests > 0)
                {
                    session.Character.SnackRequests = 0;
                }
            });

            session.Character.LastDelayRecovery = DateTime.Now.AddMilliseconds(750);
        }

        private static void Regenerate(ClientSession session, Item item)
        {
            session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6000));
            session.Character.SnackAmount++;
            session.Character.MaxSnack = 0;
            session.Character.SnackHp += item.Hp / 5;
            session.Character.SnackMp += item.Mp / 5;
            _regenerateDisposable = Observable.Timer(TimeSpan.FromMilliseconds(1800 * 5)).Subscribe(obs =>
            {
                if (session.Character.SnackHp > 0 || session.Character.SnackMp > 0)
                {
                    session.Character.SnackHp -= item.Hp / 5;
                    session.Character.SnackMp -= item.Mp / 5;
                    session.Character.SnackAmount--;
                }
            });
        }

        private static void Sync(ClientSession session)
        {
            for (session.Character.MaxSnack = 0; session.Character.MaxSnack < 5; session.Character.MaxSnack++)
            {
                if (session.Character.Hp <= 0)
                {
                    _regenerateDisposable?.Dispose();
                    session.Character.SnackHp = 0;
                    session.Character.SnackMp = 0;
                    session.Character.SnackAmount = 0;
                    return;
                }

                var hpLoad = (int)session.Character.HPLoad();
                var mpLoad = (int)session.Character.MPLoad();

                var buffRc = session.Character.GetBuff(BCardType.CardType.LeonaPassiveSkill,
                                 (byte)AdditionalTypes.LeonaPassiveSkill.IncreaseRecoveryItems)[0] / 100D;

                var hpAmount = session.Character.SnackHp + (int)(session.Character.SnackHp * buffRc);
                var mpAmount = session.Character.SnackMp + (int)(session.Character.SnackMp * buffRc);

                if (session.Character.Hp + hpAmount > hpLoad)
                {
                    hpAmount = hpLoad - session.Character.Hp;
                }

                if (session.Character.Mp + mpAmount > mpLoad)
                {
                    mpAmount = mpLoad - session.Character.Mp;
                }

                var convertRecoveryToDamage = ServerManager.RandomNumber() <
                                              session.Character.GetBuff(BCardType.CardType.DarkCloneSummon,
                                                      (byte)AdditionalTypes.DarkCloneSummon.ConvertRecoveryToDamage)[0];

                if (convertRecoveryToDamage)
                {
                    session.Character.Hp -= hpAmount;

                    if (session.Character.Hp < 1)
                    {
                        session.Character.Hp = 1;
                    }

                    if (hpAmount > 0)
                    {
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateDm(hpAmount));
                    }
                }
                else
                {
                    session.Character.Hp += hpAmount;

                    if (hpAmount > 0)
                    {
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateRc(hpAmount));
                    }
                }

                session.Character.Mp += mpAmount;

                foreach (var mate in session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive))
                {
                    hpLoad = mate.HpLoad();
                    mpLoad = mate.MpLoad();

                    hpAmount = session.Character.SnackHp;
                    mpAmount = session.Character.SnackMp;

                    if (mate.Hp + hpAmount > hpLoad)
                    {
                        hpAmount = hpLoad - (int)mate.Hp;
                    }

                    if (mate.Mp + mpAmount > mpLoad)
                    {
                        mpAmount = mpLoad - (int)mate.Mp;
                    }

                    mate.Hp += hpAmount;
                    mate.Mp += mpAmount;

                    if (hpAmount > 0)
                    {
                        session.CurrentMapInstance?.Broadcast(session, mate.GenerateRc(hpAmount));
                    }
                }

                if (session.IsConnected)
                {
                    session.SendPacket(session.Character.GenerateStat());

                    if (session.Character.Mates.Any(m => m.IsTeamMember && m.IsAlive))
                    {
                        session.SendPackets(session.Character.GeneratePst());
                    }

                    Thread.Sleep(1800);
                }
                else
                {
                    return;
                }
            }

            session.Character.SnackAmount = 0;
        }

        #endregion
    }
}