using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension
{
    public static class CharacterConfigurationExtension
    {
       public static async Task Set(ClientSession Session, CharacterConfigurationType ConfigurationType)
       {
            switch (ConfigurationType)
            {
                case CharacterConfigurationType.AutoLoot:
                    if (Session.Character.AutoLoot)
                    {
                        Session.Character.AutoLoot = false;
                        MessageExtension.SendYellow(Session, "Auto Loot has been deactivated");
                    }
                    else
                    {
                        Session.Character.AutoLoot = true;
                        MessageExtension.SendGreen(Session, "Auto Loot has been activated");
                    }
                    break;

                case CharacterConfigurationType.SafeBet:
                    if (Session.Character.SafeBet)
                    {
                        Session.Character.SafeBet = false;
                        MessageExtension.SendYellow(Session, "Safe Bet has been deactivated");
                    }
                    else
                    {
                        Session.Character.SafeBet = true;
                        MessageExtension.SendGreen(Session, "Safe Bet has been activated");
                    }
                    break;
            }
       }
    }
}