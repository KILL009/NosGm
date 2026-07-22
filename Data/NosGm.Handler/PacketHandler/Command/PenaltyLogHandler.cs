using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class PenaltyLogHandler : IPacketHandler
    {
        #region Instantiation

        public PenaltyLogHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ListPenalties(PenaltyLogPacket penaltyLogPacket)
        {
            var returnHelp = CharacterStatsPacket.ReturnHelp();
            if (penaltyLogPacket != null)
            {
                //Session.AddLogsCmd(penaltyLogPacket);
                var name = penaltyLogPacket.CharacterName;
                if (!string.IsNullOrEmpty(name))
                {
                    var character = DAOFactory.CharacterDAO.LoadByName(name);
                    if (character != null)
                    {
                        var separatorSent = false;

                        void WritePenalty(PenaltyLogDTO penalty)
                        {
                            Session.SendPacket(Session.Character.GenerateSay($"Type: {penalty.Penalty}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"AdminName: {penalty.AdminName}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"Reason: {penalty.Reason}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"DateStart: {penalty.DateStart}", 13));
                            Session.SendPacket(Session.Character.GenerateSay($"DateEnd: {penalty.DateEnd}", 13));
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                            separatorSent = true;
                        }

                        IEnumerable<PenaltyLogDTO> penaltyLogs = ServerManager.Instance.PenaltyLogs
                            .Where(s => s.AccountId == character.AccountId).ToList();

                        //PenaltyLogDTO penalty = penaltyLogs.LastOrDefault(s => s.DateEnd > DateTime.Now);
                        Session.SendPacket(Session.Character.GenerateSay("----- PENALTIES -----", 13));

                        #region Warnings

                        Session.SendPacket(Session.Character.GenerateSay("----- WARNINGS -----", 13));
                        foreach (var penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Warning)
                            .OrderBy(s => s.DateStart))
                            WritePenalty(penaltyLog);

                        if (!separatorSent)
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));

                        separatorSent = false;

                        #endregion

                        #region Mutes

                        Session.SendPacket(Session.Character.GenerateSay("----- MUTES -----", 13));
                        foreach (var penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Muted)
                            .OrderBy(s => s.DateStart))
                            WritePenalty(penaltyLog);

                        if (!separatorSent)
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));

                        separatorSent = false;

                        #endregion

                        #region Bans

                        Session.SendPacket(Session.Character.GenerateSay("----- BANS -----", 13));
                        foreach (var penaltyLog in penaltyLogs.Where(s => s.Penalty == PenaltyType.Banned)
                            .OrderBy(s => s.DateStart))
                            WritePenalty(penaltyLog);

                        if (!separatorSent)
                            Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));

                        #endregion

                        Session.SendPacket(Session.Character.GenerateSay("----- SUMMARY -----", 13));
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"Warnings: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Warning)}", 13));
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                $"Mutes: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Muted)}", 13));
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                $"Bans: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Banned)}", 13));
                        Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(returnHelp, 10));
            }
        }

        #endregion
    }
}