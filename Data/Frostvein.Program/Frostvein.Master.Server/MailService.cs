/*
 * This file is part of the Frostvein Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using Frostvein.Configuration;

using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Frostvein.Master.Server
{
    internal class MailService : ScsService, IMailService
    {
        #region Methods

        public bool Authenticate(string authKey, Guid serverId)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                return false;
            }

            if (authKey == ServerConfiguration.MasterAuthKey)
            {
                MSManager.Instance.AuthentificatedClients.Add(CurrentClient.ClientId);

                WorldServer ws = MSManager.Instance.WorldServers.Find(s => s.Id == serverId);
                if (ws != null)
                {
                    ws.MailServiceClient = CurrentClient;
                }
                return true;
            }

            return false;
        }

        public void SendMail(MailDTO mail)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return;
            }

            DAOFactory.MailDAO.InsertOrUpdate(ref mail);

            if (mail.IsSenderCopy)
            {
                AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(mail.SenderId));
                if (account?.ConnectedWorld != null)
                {
                    account.ConnectedWorld.MailServiceClient.GetClientProxy<IMailClient>().MailSent(mail);
                }
            }
            else
            {
                AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(mail.ReceiverId));
                if (account?.ConnectedWorld != null)
                {
                    account.ConnectedWorld.MailServiceClient.GetClientProxy<IMailClient>().MailSent(mail);
                }
            }
        }

       

        #endregion
    }
}