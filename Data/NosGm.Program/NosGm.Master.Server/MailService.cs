/*
 * This file is part of the NosGm Emulator Project. See AUTHORS file for Copyright information
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

using NosGm.Configuration;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.ScsServices.Service;
using System;

namespace NosGm.Master.Server
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
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)) || mail == null)
            {
                return;
            }

            if (!mail.DeliveryOperationId.HasValue || mail.DeliveryOperationId.Value == Guid.Empty)
            {
                mail.DeliveryOperationId = Guid.NewGuid();
            }

            if (mail.DeliverySource == ItemTraceSource.Unknown)
            {
                mail.DeliverySource = ItemTraceSource.Mail;
            }

            DAOFactory.MailDAO.InsertOrUpdate(ref mail);

            // When an already-completed operation is retried, the ledger returns its original
            // MailId even though the parcel row was consumed. Do not recreate or re-notify it.
            var persistedMail = mail.MailId > 0 ? DAOFactory.MailDAO.LoadById(mail.MailId) : null;
            if (persistedMail == null)
            {
                return;
            }

            if (persistedMail.IsSenderCopy)
            {
                AccountConnection account = MSManager.Instance.ConnectedAccounts
                    .Find(a => a.CharacterId.Equals(persistedMail.SenderId));
                if (account?.ConnectedWorld != null)
                {
                    account.ConnectedWorld.MailServiceClient
                        .GetClientProxy<IMailClient>()
                        .MailSent(persistedMail);
                }
            }
            else
            {
                AccountConnection account = MSManager.Instance.ConnectedAccounts
                    .Find(a => a.CharacterId.Equals(persistedMail.ReceiverId));
                if (account?.ConnectedWorld != null)
                {
                    account.ConnectedWorld.MailServiceClient
                        .GetClientProxy<IMailClient>()
                        .MailSent(persistedMail);
                }
            }
        }

        #endregion
    }
}
