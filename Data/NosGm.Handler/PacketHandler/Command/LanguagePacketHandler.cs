using NosGm.Core;
using NosGm.DAL;
using NosGm.Data.Enums;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.Packets.Packets.CommandPackets;
using System;

namespace NosGm.Handler.PacketHandler.Command
{
    public class LanguagePacketHandler : IPacketHandler
    {
        public LanguagePacketHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void SetLanguage(LanguagePacket languagePacket)
        {
            if (languagePacket == null ||
                !Language.Instance.TryNormalizeCulture(languagePacket.Culture, out var culture))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(
                    Session.GetMessageFromKey("LANGUAGE_NOT_SUPPORTED"),
                    Language.Instance.SupportedCultureList)));
                return;
            }

            var account = DAOFactory.AccountDAO.LoadById(Session.Account.AccountId);
            if (account == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    Session.GetMessageFromKey("LANGUAGE_SAVE_ERROR")));
                return;
            }

            account.Language = culture;
            if (DAOFactory.AccountDAO.InsertOrUpdate(ref account) == SaveResult.Error)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    Session.GetMessageFromKey("LANGUAGE_SAVE_ERROR")));
                return;
            }

            Session.Account.Language = culture;
            Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(
                Session.GetMessageFromKey("LANGUAGE_CHANGED"),
                culture.ToUpperInvariant())));
        }
    }
}
