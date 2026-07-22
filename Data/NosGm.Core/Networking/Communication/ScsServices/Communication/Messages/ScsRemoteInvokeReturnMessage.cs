using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using System;

namespace NosGm.Core.Networking.Communication.ScsServices.Communication.Messages
{
    /// <summary>
    ///     This message is sent as response message to a ScsRemoteInvokeMessage. It is used to send
    ///     return value of method invocation.
    /// </summary>
    [Serializable]
    public class ScsRemoteInvokeReturnMessage : ScsMessage
    {
        #region Methods

        /// <summary>
        ///     Represents this object as string.
        /// </summary>
        /// <returns>String representation of this object</returns>
        public override string ToString() => $"ScsRemoteInvokeReturnMessage: Returns {ReturnValue}, Exception = {RemoteException}";

        #endregion

        #region Properties

        /// <summary>
        ///     Parameters that may have been modified by out or ref in the call to the method
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        ///     If any exception occured during method invocation, this field contains Exception object.
        ///     If no exception occured, this field is null.
        /// </summary>
        public ScsRemoteException RemoteException { get; set; }

        /// <summary>
        ///     Return value of remote method invocation.
        /// </summary>
        public object ReturnValue { get; set; }

        #endregion
    }
}