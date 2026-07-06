using Frostvein.Domain;
using Frostvein.XMLModel.Objects;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension.Reputation
{
    public static class ReputationExtension
    {
        public static int GetReputation(ClientSession Session)
        {
            if (Session.Character.Reputation >= 5000001 && Session.Character.Icon == 0)
            {
                switch (Session.Character.IsReputationHero())
                {
                    case 1: return 28;
                    case 2: return 29;
                    case 3: return 30;
                    case 4: return 31;
                    case 5: return 32;
                }
            }
            if (Session.Character.Icon == 0)
            {
                if (Session.Character.Reputation <= 50) return (int)ReputationType.GreenLeaf;
                if (Session.Character.Reputation <= 150) return (int)ReputationType.GreenLeaf2;
                if (Session.Character.Reputation <= 250) return (int)ReputationType.GreenLeaf3;
                if (Session.Character.Reputation <= 500) return (int)ReputationType.GreenDagger;
                if (Session.Character.Reputation <= 750) return (int)ReputationType.BlueDagger;
                if (Session.Character.Reputation <= 1000) return (int)ReputationType.RedDagger;
                if (Session.Character.Reputation <= 2250) return (int)ReputationType.GreenHammer;
                if (Session.Character.Reputation <= 3500) return (int)ReputationType.BlueHammer;
                if (Session.Character.Reputation <= 5000) return (int)ReputationType.RedHammer;
                if (Session.Character.Reputation <= 9500) return (int)ReputationType.GreenSword;
                if (Session.Character.Reputation <= 19000) return (int)ReputationType.BlueSword;
                if (Session.Character.Reputation <= 25000) return (int)ReputationType.RedSword;
                if (Session.Character.Reputation <= 40000) return (int)ReputationType.GreenHelmet;
                if (Session.Character.Reputation <= 60000) return (int)ReputationType.BlueHelmet;
                if (Session.Character.Reputation <= 85000) return (int)ReputationType.RedHelmet;
                if (Session.Character.Reputation <= 115000) return (int)ReputationType.GreenFist;
                if (Session.Character.Reputation <= 150000) return (int)ReputationType.BlueFist;
                if (Session.Character.Reputation <= 190000) return (int)ReputationType.RedFist;
                if (Session.Character.Reputation <= 235000) return (int)ReputationType.GreenShield;
                if (Session.Character.Reputation <= 285000) return (int)ReputationType.BlueShield;
                if (Session.Character.Reputation <= 350000) return (int)ReputationType.RedShield;
                if (Session.Character.Reputation <= 500000) return (int)ReputationType.GreenMoon;
                if (Session.Character.Reputation <= 1500000) return (int)ReputationType.BlueMoon;
                if (Session.Character.Reputation <= 2500000) return (int)ReputationType.RedMoon;
                if (Session.Character.Reputation <= 3750000) return (int)ReputationType.GreenStar;
                return Session.Character.Reputation <= 5000000 ? (int)ReputationType.BlueStar : (int)ReputationType.RedStar;
            }

            return Session.Character.Icon;
        }
    }
}
