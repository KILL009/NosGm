using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class DamageConvertingSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.DamageConvertingSkill;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SubType = evnt.BCard.SubType;
            var BCardId = evnt.BCard.BCardId;

            if (SubType.Equals((byte)AdditionalTypes.DamageConvertingSkill.TransferInflictedDamage))
            {
                if (caster.Character != null)
                {
                    caster.Character.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SACRIFICES_THEMSELVES"), caster.Character.Name, target.Character?.Name ?? target.Mate?.Name ?? target.MapNpc?.Name ?? target.MapMonster?.Name), 3));
                    target.Character?.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SACRIFICES_THEMSELVES"), caster.Character.Name, target.Character.Name), 3));

                    IDisposable bcardDisposable = null;
                    bcardDisposable = Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(s =>
                    {
                        if (target.BCardDisposables[BCardId] != bcardDisposable)
                        {
                            bcardDisposable.Dispose();
                            return;
                        }
                        if (!caster.Character.Session.HasCurrentMapInstance || Map.GetDistance(new MapCell { X = target.PositionX, Y = target.PositionY }, new MapCell { X = caster.PositionX, Y = caster.PositionY }) > 10 || target.MapInstance != caster.MapInstance || !caster.Buffs.Any(c => c.Card.CardId == 546))
                        {
                            caster.Character.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SACRIFICE_OUT_OF_RANGE")), 3));
                            target.Character?.Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SACRIFICE_OUT_OF_RANGE")), 3));
                            target?.ClearSacrificeBuff();
                            target?.BCardDisposables[BCardId]?.Dispose();
                        }
                    });
                    target.BCardDisposables[BCardId] = bcardDisposable;
                }
            }
        }
    }
}
