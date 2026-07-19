using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data.Enums;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.Packets.Packets.CommandPackets;
using System;

namespace Frostvein.Handler.PacketHandler.Command
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
