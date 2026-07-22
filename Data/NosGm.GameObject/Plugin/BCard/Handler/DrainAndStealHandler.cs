using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class DrainAndStealHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.DrainAndSteal;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var ThirdData = evnt.BCard.ThirdData;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;

            if (SubType == (byte)AdditionalTypes.DrainAndSteal.ConvertEnemyHPToMP)
            {
                var bonus = 0;
                if (evnt.BCard.IsLevelScaled)
                {
                    bonus = casterLevel * (firstData - 1);
                }
                else
                {
                    bonus = firstData;
                }

                bonus = target.GetDamage(bonus, caster, true, true);
                if (bonus > 0)
                {
                    target.MapInstance?.Broadcast(target.GenerateDm(bonus));
                    target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                    if (caster.Mp + bonus > caster.MPLoad())
                    {
                        caster.Mp = (int)caster.MPLoad();
                    }
                    else
                    {
                        caster.Mp += bonus;
                    }

                    caster.Character?.Session?.SendPacket(caster.Character?.GenerateStat());
                }
            }
            else if (SubType == (byte)AdditionalTypes.DrainAndSteal.LeechEnemyHP)
            {
                if (SecondData == 0)
                {
                    return; // Wtf !? !? !!!
                }

                if (ServerManager.RandomNumber() < FirstData)
                {
                    if (target.Hp > 1
                        && target.MapInstance != null)
                    {
                        var amount = SecondData;

                        if (amount >= target.Hp)
                        {
                            amount = target.Hp - 1;
                        }

                        if (caster.Hp + amount > caster.HpMax)
                        {
                            amount = caster.HpMax - caster.Hp;
                        }

                        target.Hp -= amount;
                        caster.Hp += amount;

                        caster.MapInstance?.Broadcast(caster.GenerateRc(amount));
                        caster.Character?.Session?.SendPacket(caster.Character?.GenerateStat());
                        target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                    }
                }
            }
            else if (SubType == (byte)AdditionalTypes.DrainAndSteal.LeechEnemyMP)
            {
                if (ThirdData != 0)
                {
                    return;
                }

                if (ServerManager.RandomNumber() < FirstData)
                {
                    if (target.Mp > 1
                        && target.MapInstance != null)
                    {
                        var amount = SecondData;

                        if (amount >= target.Mp)
                        {
                            amount = target.Mp - 1;
                        }

                        target.Mp -= amount;
                        caster.Mp += amount;

                        if (caster.Mp > caster.MpMax)
                        {
                            caster.Mp = caster.MpMax;
                        }

                        caster.Character?.Session?.SendPacket(caster.Character?.GenerateStat());
                        target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                    }
                }
            }
        }
    }
}
