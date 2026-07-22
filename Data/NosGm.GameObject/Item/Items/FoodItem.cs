using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace NosGm.GameObject
{
    public class FoodItem : Item
    {
        #region Instantiation

        public FoodItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Properties

        private static IDisposable _regenerateDisposable { get; set; }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
            {
                session.SendPacket("msg 4 You cannot do that here");
                return;
            }
            if (session.Character.IsVehicled)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
            {
                return;
            }

            if (session.Character.MapId == 2010 || session.Character.MapId == 2005 || session.Character.MapId == 2015)
            {
                session.SendPacket(session.Character.GenerateSay("Can't use Foods in this Map.", 11));
                return;
            }

            if ((DateTime.Now - session.Character.LastPotion).TotalMilliseconds < 750)
            {
                return;
            }

            session.Character.LastPotion = DateTime.Now;
            var item = inv.Item;
            switch (Effect)
            {
                default:
                    if (session.Character.Hp <= 0)
                    {
                        return;
                    }

                    if (item.VNum == 2291 || item.VNum == 10035)
                    {
                        if (!session.Character.IsSitting)
                        {
                            session.Character.Rest();
                        }

                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                            session.Character.CharacterId, 6000));

                        session.Character.SpPoint += 1500;

                        if (session.Character.SpPoint > 10000)
                        {
                            session.Character.SpPoint = 10000;
                        }

                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(session.Character.GenerateSpPoint());
                        return;
                    }

                    if (session.Character.FoodAmount < 0)
                    {
                        session.Character.FoodAmount = 0;
                    }

                    var amount = session.Character.FoodAmount;

                    if (amount < 5)
                    {
                        if (item.BCards.Find(s =>
                                s.Type == (byte)BCardType.CardType.HPMP &&
                                s.SubType == (byte)AdditionalTypes.HPMP.ReceiveAdditionalHP &&
                                s.FirstData > 0) is BCard
                            AdditionalHpBCard)
                        {
                            // MaxAdditionalHpPercent = AdditionalHp.SecondData;
                            double AdditionalHp = 0;
                            if (session.Character.BattleEntity.AdditionalHp + AdditionalHpBCard.FirstData <=
                                session.Character.HPLoad() * 0.2)
                            {
                                AdditionalHp = AdditionalHpBCard.FirstData;
                            }
                            else if (session.Character.BattleEntity.AdditionalHp < session.Character.HPLoad() * 0.2)
                            {
                                AdditionalHp = session.Character.HPLoad() * 0.2 -
                                               session.Character.BattleEntity.AdditionalHp;
                            }

                            if (AdditionalHp > 0 && AdditionalHp <= AdditionalHpBCard.FirstData)
                            {
                                session.Character.FoodAmount++;
                                session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                                    session.Character.CharacterId, 6000));
                                session.Character.BattleEntity.AdditionalHp += AdditionalHp;
                                session.SendPacket(session.Character.GenerateAdditionalHpMp());
                                session.SendPacket(session.Character.GenerateStat());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                Observable.Timer(TimeSpan.FromMilliseconds(1800))
                                    .Subscribe(s => session.Character.FoodAmount--);
                            }

                            return;
                        }

                        if (item.BCards.Find(s =>
                                s.Type == (byte)BCardType.CardType.HPMP &&
                                s.SubType == (byte)AdditionalTypes.HPMP.ReceiveAdditionalMP &&
                                s.FirstData < 0) is BCard
                            AdditionalMpBCard)
                        {
                            // MaxAdditionalMpPercent = AdditionalMp.SecondData;
                            double AdditionalMp = 0;
                            if (session.Character.BattleEntity.AdditionalMp + -AdditionalMpBCard.FirstData <=
                                session.Character.MPLoad() * 0.2)
                            {
                                AdditionalMp = -AdditionalMpBCard.FirstData;
                            }
                            else if (session.Character.BattleEntity.AdditionalMp < session.Character.MPLoad() * 0.2)
                            {
                                AdditionalMp = session.Character.MPLoad() * 0.2 -
                                               session.Character.BattleEntity.AdditionalMp;
                            }

                            if (AdditionalMp > 0 && AdditionalMp <= -AdditionalMpBCard.FirstData)
                            {
                                session.Character.FoodAmount++;
                                session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                                    session.Character.CharacterId, 6000));
                                session.Character.BattleEntity.AdditionalMp += AdditionalMp;
                                session.SendPacket(session.Character.GenerateAdditionalHpMp());
                                session.SendPacket(session.Character.GenerateStat());
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                Observable.Timer(TimeSpan.FromMilliseconds(1800))
                                    .Subscribe(s => session.Character.FoodAmount--);
                            }

                            return;
                        }
                    }

                    if (!session.Character.IsSitting)
                    {
                        session.Character.Rest();
                    }

                    session.Character.Mates.Where(s => s.IsTeamMember).ToList().ForEach(m =>
                                    session.CurrentMapInstance?.Broadcast(m.GenerateRest(true)));

                    if (amount < 5)
                    {
                        if (!session.Character.IsSitting)
                        {
                            return;
                        }

                        var workerThread = new Thread(() => Regenerate(session, item));
                        workerThread.Start();
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else
                    {
                        session.SendPacket(session.Character.Gender == GenderType.Female
                            ? session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_HUNGRY_FEMALE"), 1)
                            : session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_HUNGRY_MALE"), 1));
                    }

                    if (amount == 0)
                    {
                        if (!session.Character.IsSitting)
                        {
                            return;
                        }

                        var workerThread2 = new Thread(() => Sync(session));
                        workerThread2.Start();
                    }

                    break;
            }
        }

        private static void Regenerate(ClientSession session, Item item)
        {
            session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 6000));
            session.Character.FoodAmount++;
            session.Character.MaxFood = 0;
            session.Character.FoodHp += item.Hp / 5;
            session.Character.FoodMp += item.Mp / 5;
            _regenerateDisposable = Observable.Timer(TimeSpan.FromMilliseconds(1800 * 5)).Subscribe(obs =>
            {
                if (session.Character.FoodHp > 0 || session.Character.FoodMp > 0)
                {
                    session.Character.FoodHp -= item.Hp / 5;
                    session.Character.FoodMp -= item.Mp / 5;
                    session.Character.FoodAmount--;
                }
            });
        }

        private static void Sync(ClientSession session)
        {
            for (session.Character.MaxFood = 0; session.Character.MaxFood < 5; session.Character.MaxFood++)
            {
                if (session.Character.Hp <= 0 || !session.Character.IsSitting)
                {
                    _regenerateDisposable?.Dispose();
                    session.Character.FoodAmount = 0;
                    session.Character.FoodHp = 0;
                    session.Character.FoodMp = 0;
                    return;
                }

                var hpLoad = (int)session.Character.HPLoad();
                var mpLoad = (int)session.Character.MPLoad();

                var buffRc = session.Character.GetBuff(BCardType.CardType.LeonaPassiveSkill,
                                 (byte)AdditionalTypes.LeonaPassiveSkill.IncreaseRecoveryItems)[0] / 100D;

                var hpAmount = session.Character.FoodHp + (int)(session.Character.FoodHp * buffRc);
                var mpAmount = session.Character.FoodMp + (int)(session.Character.FoodMp * buffRc);

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

                foreach (var mate in session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive && s.IsSitting))
                {
                    hpLoad = mate.HpLoad();
                    mpLoad = mate.MpLoad();

                    hpAmount = session.Character.FoodHp;
                    mpAmount = session.Character.FoodMp;

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

                    if (session.Character.Mates.Any(m => m.IsTeamMember && m.IsAlive && m.IsSitting))
                    {
                        session.SendPackets(session.Character.GeneratePst());
                    }

                    Thread.Sleep(1800);
                }
            }
        }

        #endregion
    }
}