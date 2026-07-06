using Game.Configuration.BCards;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class TransfoHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Transfo;

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

            if (SubType.Equals((byte)AdditionalTypes.Transfo.ToMorph) && target.Character != null)
            {
                if (target.Character.Morph == SecondData + 1000) return;

                Card morphCard = ServerManager.GetCard(CardId.Value);

                if (morphCard == null)
                {
                    return;
                }


                //target.Character.OldMorph = target.Character.Morph;
                target.Character.IsVehicled = true;
                target.Character.VehicleSpeed = 21;
                target.Character.LoadSpeed();
                target.Character.MorphUpgrade = 0;
                target.Character.MorphUpgrade2 = 0;
                target.Character.Morph = SecondData + 1000;
                target.Character.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, target.Character.CharacterId, 196), target.Character.PositionX, target.Character.PositionY);
                target.Character.Session.CurrentMapInstance?.Broadcast(target.Character.GenerateCMode());
                target.Character.Session.SendPacket(target.Character.GenerateCond());
                target.Character.LastSpeedChange = DateTime.Now;
                target.Character.Mates.Where(s => s.IsTeamMember).ToList()
                    .ForEach(s => target.Character.Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                target.Character.DragonModeObservable?.Dispose();
                target.Character.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                target.Character.DragonModeObservable = Observable.Timer(TimeSpan.FromSeconds(morphCard.Duration * 0.1)).Subscribe(s =>
                {
                    target.Character.IsVehicled = false;
                    target.Character.VehicleSpeed = 0;
                    target.Character.LoadSpeed();
                    target.Character.MorphUpgrade = 0;
                    target.Character.MorphUpgrade2 = 0;
                    //target.Character.Morph = target.Character.OldMorph;
                    target.Character.Session.SendPacket(target.Character.GenerateCMode());
                    target.Character.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, target.Character.CharacterId, 196));
                    target.Character.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    foreach (Mate teamMate in target.Character.Mates?.Where(m => m != null && m.IsTeamMember))
                    {
                        teamMate.PositionX = target.Character.PositionX;
                        teamMate.PositionY = target.Character.PositionY;
                        teamMate.UpdateBushFire();

                        if (target.Character.MapInstance?.Sessions != null)
                        {
                            Parallel.ForEach(target.Character.MapInstance.Sessions.Where(s => s?.Character != null), s =>
                            {
                                if (ServerManager.Instance.ChannelId != 51 || target.Character.Faction == s.Character.Faction)
                                {
                                    s.SendPacket(teamMate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                }
                                else
                                {
                                    s.SendPacket(teamMate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                }
                            });
                        }

                        target.Character.Session?.SendPacket(target.Character.GeneratePinit());
                        target.Character.Mates?.ForEach(s => target.Character.Session?.SendPacket(s.GenerateScPacket()));
                        target.Character.Session?.SendPackets(target.Character.GeneratePst());
                    }
                });
            }
        }
    }
}
