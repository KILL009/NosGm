using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface IMailDAO
    {
        #region Methods

        DeleteResult DeleteById(long mailId);

        SaveResult InsertOrUpdate(ref MailDTO mail);

        IEnumerable<MailDTO> LoadAll();

        MailDTO LoadById(long mailId);

        IEnumerable<MailDTO> LoadSentByCharacter(long characterId);

        Task<IEnumerable<MailDTO>> LoadSentToCharacterAsync(long characterId);

        void MarkDeliveryClaimed(long mailId, Guid itemInstanceId);

        #endregion
    }
}
