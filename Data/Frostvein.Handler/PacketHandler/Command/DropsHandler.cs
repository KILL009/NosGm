using MongoDB.Driver.Linq;
using Frostvein.Configuration;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Core.Networking.Communication.Scs.Server;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class DropsHandler : IPacketHandler
    {
        #region Instantiation

        public DropsHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Drops(DropsPacket dropsPacket)
        {
            if (Session.Character.LastDropPacket.AddSeconds(10) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 10 seconds before doing that again");
                return;
            }

            var counter = 0;

            var mapTypeMap = DAOFactory.MapTypeMapDAO.LoadByMapId(Session.Character.MapInstance.Map.MapId).FirstOrDefault();

            var items = DAOFactory.DropDAO.LoadByMapOrMonsters((mapTypeMap == default(MapTypeMapDTO) ? (short)0 : mapTypeMap.MapTypeId), Session.Character.MapInstance.Monsters.GroupBy(x => x.MonsterVNum).Select(x => x.First().MonsterVNum).ToList()).ToArray();
            if (items?.Length == 0)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("No Map- or MonsterDrops for this Map available"));
                return;
            }

            var entrys = items.GroupBy(x => x.ItemVNum).Select(x => x.First()).ToArray();
            var drops = entrys.Select(x => $"{counter++}.{x.ItemVNum}.1.{x.Amount}.0.0");

            Session.SendPacket($"f_stash_all {entrys.Length} {string.Join(" ", drops)}");
        }

        #endregion
    }
}