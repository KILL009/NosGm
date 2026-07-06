using MongoDB.Driver;
using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension
{
    public static class SealedVesselEventExtension
    {
       public static void GenerateDrop(ClientSession Session, MapMonster monsterToAttack, long? Owner)
       {
            if (EventConfiguration.IsActivated && EventConfiguration.SealedVessel && monsterToAttack.Monster.IsSealedVessel)
            {
                DropDTO SpecialDrop1 = new()
                {
                    Amount = 1,
                    ItemVNum = 1097,
                };
                if (Session.Character.HasBuff(5003) || Session.Character.HasBuff(5004) || Session.Character.HasBuff(5005))
                {
                    Session.Character.GiftAdd(SpecialDrop1.ItemVNum, (short)SpecialDrop1.Amount);
                }
                else
                {
                    Session.CurrentMapInstance.DropItemByMonster(Owner, SpecialDrop1, monsterToAttack.MapX, monsterToAttack.MapY);
                }
            }
        }
    }
}
