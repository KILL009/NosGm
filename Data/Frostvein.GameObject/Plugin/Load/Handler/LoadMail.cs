using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load.Handler
{
    public static class LoadMail
    {
        public static void LoadMailProcess()
        {
            List<ClientSession> sessions = ServerManager.Instance.Sessions.Where(s => s?.IsConnected == true).ToList();

            foreach (ClientSession session in sessions)
            {
                IEnumerable<MailDTO> newMails = DAOFactory.MailDAO.LoadSentToCharacterAsync(session.Character.CharacterId)
               .ConfigureAwait(false)
               .GetAwaiter()
               .GetResult();

                if (newMails.Any())
                {
                    foreach (MailDTO mail in newMails)
                    {
                        //Check if the Mail is existing
                        if (!session.Character.MailList.Any(m => m.Value.MailId == mail.MailId))
                        {
                            //Add the Mail if it's new
                            session.Character.MailList.Add((session.Character.MailList.Count > 0 ? session.Character.MailList.OrderBy(s => s.Key).Last().Key : 0) + 1, mail);

                            if (mail.AttachmentVNum != null)
                            {
                                //Generate the Packet for new Parcels
                                MessageExtension.SendBubble(session, "You received a new Mail!");
                                session.SendPacket(session.Character.GenerateParcel(mail));
                            }
                            else
                            {
                                if (!mail.IsOpened)
                                {
                                    //Logic for unopened Parcels?
                                }
                                session.SendPacket(session.Character.GeneratePost(mail, 1));
                            }
                        }
                    }
                }
            }
        }
    }
}
