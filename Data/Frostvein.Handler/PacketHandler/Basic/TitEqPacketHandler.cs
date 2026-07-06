using Frostvein.Packets.Packets.ServerPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class TitEqPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TitEqPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task TitEqHandlerAsync(TitEqPacket e)
        {
            var aa = Session.Character.Title.FirstOrDefault(s => s.TitleVnum == e.ItemVnum);

            if (aa == null) return;

            switch (e.Type)
            {
                case 1:
                    //show
                    foreach (var a in Session.Character.Title.Where(s => s != aa))
                        switch (a.Stat)
                        {
                            case 7 when a != aa:
                                a.Stat = 5;
                                break;
                            case 3 when a != aa:
                                a.Stat = 1;
                                break;
                                /*case 5 when a != aa:
                                        a.Stat = 7;
                                        break;*/
                        }

                    aa.Stat = (byte)(aa.Stat == 1 ? 3 : aa.Stat == 5 ? 7 : aa.Stat == 7 ? 5 : 1);
                    break;

                case 2:
                    //effect
                    foreach (var a in Session.Character.Title)
                        switch (a.Stat)
                        {
                            case 7 when a != aa:
                                a.Stat = 3;
                                break;
                            case 5 when a != aa:
                                a.Stat = 1;
                                break;
                                /*case 3 when a != aa:
                                        a.Stat = 7;
                                        break;*/
                        }

                    aa.Stat = (byte)(aa.Stat == 1 ? 5 : aa.Stat == 3 ? 7 : aa.Stat == 7 ? 3 : 1);

                    break;

                default:
                    return;
            }

            Session.SendPacket(Session.Character.GenerateTitle());
            Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTitInfo());
            Session.Character.GetEffectFromTitle();

            Session.SendPackets(Session.Character.GenerateStatChar());
            // Session.SendPacket(Session.Character.GenerateCInfo());            
            // Session.SendPacket(Session.Character.GenerateCMode());
            // Session.SendPacket(Session.Character.GenerateEq());
            // Session.SendPacket(Session.Character.GenerateEquipment());
            // Session.SendPacket(Session.Character.GenerateLev());
            Session.SendPacket(Session.Character.GenerateStat());
            // Session.SendPacket(Session.Character.GenerateAt());
            // Session.SendPacket(Session.Character.GenerateCond());
            // Session.SendPacket(Session.Character.GeneratePairy());
            Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTitInfo());
            // Session.SendPacket(Character.GenerateAct());
            // Session.SendPacket(Session.Character.GenerateScpStc());
        }

        #endregion
    }
}