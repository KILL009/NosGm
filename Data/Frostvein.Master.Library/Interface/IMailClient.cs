using Frostvein.Data;

namespace Frostvein.Master.Library.Interface
{
    public interface IMailClient
    {
        #region Methods

        void MailSent(MailDTO mail);

        #endregion
    }
}