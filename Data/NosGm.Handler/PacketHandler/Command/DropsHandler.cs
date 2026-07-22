using MongoDB.Driver.Linq;
using NosGm.Configuration;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Core.Networking.Communication.Scs.Server;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Linq;
using static System.Collections.Specialized.BitVector32;

namespace NosGm.Handler.PacketHandler.Command
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