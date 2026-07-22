using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class SearchItemHandler : IPacketHandler
    {
        #region Instantiation

        public SearchItemHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SearchItem(SearchItemPacket searchItemPacket)
        {
            if (searchItemPacket != null)
            {
                var contents = searchItemPacket.Contents;
                //Session.AddLogsCmd(searchItemPacket);
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

                IEnumerable<ItemDTO> itemlist = DAOFactory.ItemDAO.FindByName(name).OrderBy(s => s.VNum)
                    .Skip(page * 200).Take(200).ToList();
                if (itemlist.Any())
                    foreach (var item in itemlist)
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"[SearchItem:{page}]Item: {(string.IsNullOrEmpty(item.Name) ? "none" : item.Name)} VNum: {item.VNum}",
                            12));
                else
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_NOT_FOUND"), 11));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SearchItemPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}