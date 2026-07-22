using Game.Configuration.BCards;
using NosGm.GameObject;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class DragonSkillsHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.MartialArts;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var CardId = evnt.BCard.CardId;
            var SubType = evnt.BCard.SubType;

            switch (SubType)
            {
                case ((byte)AdditionalTypes.MartialArts.TransformationInverted):
                case ((byte)AdditionalTypes.MartialArts.Transformation):
                    Character user = caster.Character ?? target.Character;

                    if (user == null)
                    {
                        break;
                    }

                    if (user.Morph == 30)
                    {
                        user.Morph = 29;
                        user.Session.SendPacket(user.GenerateCMode());
                        user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                        user.DragonModeObservable?.Dispose();
                        user.RemoveBuff(676);
                        user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));
                    }
                    else if (user.Morph == 29)
                    {
                        if (!CardId.HasValue)
                        {
                            break;
                        }

                        Card morphCard = ServerManager.GetCard(CardId.Value);

                        if (morphCard == null)
                        {
                            return;
                        }
                        user.Morph = 30;
                        user.Session.SendPacket(user.GenerateCMode());
                        user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                        user.DragonModeObservable?.Dispose();
                        user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));

                        user.DragonModeObservable = Observable.Timer(TimeSpan.FromSeconds(morphCard.Duration * 0.1)).Subscribe(s =>
                        {
                            user.Morph = 29;
                            user.Session.SendPacket(user.GenerateCMode());
                            user.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, user.CharacterId, 196));
                            user.Session.SendPacket(StaticPacketHelper.Cancel(2, user.CharacterId));
                        });
                    }

                    break;
            }
        }
    }
}
