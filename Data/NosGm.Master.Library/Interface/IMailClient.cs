using NosGm.Data;

namespace NosGm.Master.Library.Interface
{
    public interface IMailClient
    {
        #region Methods

        void MailSent(MailDTO mail);

        #endregion
    }
}