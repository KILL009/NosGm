using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Extension.Message;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject.Plugin.Load.Handler
{
    public static class LoadMail
    {
        /// <summary>
        /// Loads mail only for the session that is currently entering the World.
        /// The previous implementation scanned every connected session and issued
        /// one database query per player on every login, turning staged logins into
        /// O(N²) database work under load.
        /// </summary>
        public static void LoadMailProcess(ClientSession session)
        {
            if (session?.IsConnected != true ||
                !session.HasSelectedCharacter ||
                session.Character == null)
            {
                return;
            }

            IEnumerable<MailDTO> newMails = DAOFactory.MailDAO
                .LoadSentToCharacterAsync(session.Character.CharacterId)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            foreach (MailDTO mail in newMails)
            {
                // Skip mail already present in the in-memory mailbox.
                if (session.Character.MailList.Any(m => m.Value.MailId == mail.MailId))
                {
                    continue;
                }

                int nextMailIndex = session.Character.MailList.Count > 0
                    ? session.Character.MailList.OrderBy(s => s.Key).Last().Key + 1
                    : 1;
                session.Character.MailList.Add(nextMailIndex, mail);

                if (mail.AttachmentVNum != null)
                {
                    MessageExtension.SendBubble(session, "You received a new Mail!");
                    session.SendPacket(session.Character.GenerateParcel(mail));
                    continue;
                }

                if (!mail.IsOpened)
                {
                    // Reserved for unopened-mail notification logic.
                }

                session.SendPacket(session.Character.GeneratePost(mail, 1));
            }
        }
    }
}
