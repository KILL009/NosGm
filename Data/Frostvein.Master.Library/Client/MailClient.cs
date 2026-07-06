using Frostvein.Data;
using Frostvein.Master.Library.Interface;
using System.Threading.Tasks;

namespace Frostvein.Master.Library.Client
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