using NosGm.Data;
using NosGm.Master.Library.Interface;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    internal class MailClient : IMailClient
    {
        #region Methods

        public void MailSent(MailDTO mail)
        {
            Task.Run(() => MailServiceClient.Instance.OnMailSent(mail));
        }

        #endregion
    }
}