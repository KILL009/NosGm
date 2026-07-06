using Frostvein.Data;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;
using System.Threading.Tasks;

namespace Frostvein.Master.Library.Interface
{
    [ScsService(Version = "1.1.0.0")]
    public interface IMailService
    {
        #region Methods

        /// <summary>
        ///     Authenticates a Client to the Service
        /// </summary>
        /// <param name="authKey">The private Authentication key</param>
        /// <param name="serverId"></param>
        /// <returns>true if successful, else false</returns>
        bool Authenticate(string authKey, Guid serverId);

        /// <summary>
        ///     Send Mail
        /// </summary>
        /// <param name="mail"></param>
        void SendMail(MailDTO mail);

        #endregion
    }
}