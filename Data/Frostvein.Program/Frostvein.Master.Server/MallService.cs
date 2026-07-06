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
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Data;
using Frostvein.Master.Library.Interface;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Frostvein.Master.Server
{
    internal class MallService : ScsService, IMallService
    {
        #region Methods

        public bool Authenticate(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                return false;
            }

            if (authKey == ServerConfiguration.MasterAuthKey)
            {
                MSManager.Instance.AuthentificatedClients.Add(CurrentClient.ClientId);
                return true;
            }

            return false;
        }
        //Api
        public IEnumerable<CharacterDTO> GetCharacters(long accountId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }

            return DAOFactory.CharacterDAO.LoadByAccount(accountId);
        }

        public ClientSession GetCharacterSession(string characterName)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }

            return ServerManager.Instance.GetSessionByCharacterName(characterName);
        }

        public CharacterDTO GetCharacter(long characterId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }
            return DAOFactory.CharacterDAO.LoadById(characterId);
        }


        public void SendItem(long characterId, MallItem item)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return;
            }

            MailDTO mailDTO = new MailDTO
            {
                AttachmentAmount = (short)item.Amount,
                AttachmentRarity = item.Rare,
                AttachmentUpgrade = item.Upgrade,
                AttachmentDesign = item.Design,
                AttachmentVNum = item.ItemVNum,
                Date = DateTime.Now,
                EqPacket = "",
                IsOpened = false,
                IsSenderCopy = false,
                Message = "",
                ReceiverId = characterId,
                SenderId = characterId,
                Title = "ItemMall"
            };

            DAOFactory.MailDAO.InsertOrUpdate(ref mailDTO);

            MailDTO mail = new MailDTO
            {
                AttachmentAmount = mailDTO.AttachmentAmount,
                AttachmentRarity = mailDTO.AttachmentRarity,
                AttachmentUpgrade = mailDTO.AttachmentUpgrade,
                AttachmentDesign = mailDTO.AttachmentDesign,
                AttachmentVNum = mailDTO.AttachmentVNum,
                Date = mailDTO.Date,
                EqPacket = mailDTO.EqPacket,
                IsOpened = mailDTO.IsOpened,
                IsSenderCopy = mailDTO.IsSenderCopy,
                MailId = mailDTO.MailId,
                Message = mailDTO.Message,
                ReceiverId = mailDTO.ReceiverId,
                SenderClass = mailDTO.SenderClass,
                SenderGender = mailDTO.SenderGender,
                SenderHairColor = mailDTO.SenderHairColor,
                SenderHairStyle = mailDTO.SenderHairStyle,
                SenderId = mailDTO.SenderId,
                SenderMorphId = mailDTO.SenderMorphId,
                Title = mailDTO.Title
            };

            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(mail.ReceiverId));
            if (account?.ConnectedWorld != null)
            {
                account.ConnectedWorld.MailServiceClient.GetClientProxy<IMailClient>().MailSent(mail);
            }
        }

        public void SendStaticBonus(long characterId, MallStaticBonus item) => throw new NotImplementedException();

        public AccountDTO ValidateAccount(string userName, string passHash)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passHash))
            {
                return null;
            }

            AccountDTO account = DAOFactory.AccountDAO.LoadByName(userName);

            if (account?.Password == passHash)
            {
                return account;
            }
            return null;
        }

        #endregion
    }
}