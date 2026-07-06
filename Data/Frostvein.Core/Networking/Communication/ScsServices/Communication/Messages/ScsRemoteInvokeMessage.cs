using Frostvein.Core.Networking.Communication.Scs.Communication.Messages;
using System;

namespace Frostvein.Core.Networking.Communication.ScsServices.Communication.Messages
{
    /// <summary>
    ///     This message is sent to invoke a method of a remote application.
    /// </summary>
    [Serializable]
    public class ScsRemoteInvokeMessage : ScsMessage
    {
        #region Methods

        /// <summary>
        ///     Represents this object as string.
        /// </summary>
        /// <returns>String representation of this object</returns>
        public override string ToString() => $"ScsRemoteInvokeMessage: {ServiceClassName}.{MethodName}(...)";

        #endregion

        #region Properties

        /// <summary>
        ///     Method of remote application to invoke.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        ///     Parameters of method.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        ///     Name of the remove service class.
        /// </summary>
        public string ServiceClassName { get; set; }

        #endregion
    }
}