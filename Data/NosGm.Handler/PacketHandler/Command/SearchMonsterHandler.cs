using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class SearchMonsterHandler : IPacketHandler
    {
        #region Instantiation

        public SearchMonsterHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SearchMonster(SearchMonsterPacket searchMonsterPacket)
        {
            if (searchMonsterPacket != null)
            {
                var contents = searchMonsterPacket.Contents;
                //Session.AddLogsCmd(searchMonsterPacket);
                var name = "";
                byte page = 0;
                if (!string.IsNullOrEmpty(contents))
                {
                    var packetsplit = contents.Split(' ');
                    var withPage = byte.TryParse(packetsplit[0], out page);
                    name = packetsplit.Length == 1 && withPage
                        ? ""
                        : packetsplit.Skip(withPage ? 1 : 0).Aggregate((a, b) => a + ' ' + b);
                }

                IEnumerable<NpcMonsterDTO> monsterlist = DAOFactory.NpcMonsterDAO.FindByName(name)
                    .OrderBy(s => s.NpcMonsterVNum).Skip(page * 200).Take(200).ToList();
                if (monsterlist.Any())
                    foreach (var npcMonster in monsterlist)
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"[SearchMonster:{page}]Monster: {(string.IsNullOrEmpty(npcMonster.Name) ? "none" : npcMonster.Name)} VNum: {npcMonster.NpcMonsterVNum}",
                            12));
                else
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MONSTER_NOT_FOUND"), 11));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SearchMonsterPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}