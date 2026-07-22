using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class WolfMasterHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.WolfMaster;

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

            Character user = caster.Character != null ? caster.Character : target.Character;

            if (user == null)
            {
                return;
            }

            switch (SubType)
            {
                case ((byte)AdditionalTypes.WolfMaster.AddUltimatePoints):
                    {
                        user.AddUltimatePoints((short)FirstData);
                        user.Session.SendPacket(user.GenerateFtPtPacket());
                        user.AddWolfBuffs();
                    }
                    break;

                case (byte)AdditionalTypes.WolfMaster.CanExecuteUltimateSkills:
                    {
                        user.Session.SendPacket(user.GenerateFtPtPacket());
                        user.Session.SendPackets(user.GenerateQuicklist());
                    }
                    break;
            }
        }
    }
}
