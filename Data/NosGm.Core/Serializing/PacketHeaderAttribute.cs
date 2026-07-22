using NosGm.Domain;
using System;

namespace NosGm.Core
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class PacketHeaderAttribute : Attribute
    {
        #region Instantiation

        public PacketHeaderAttribute(params string[] identification) => Identification = identification;

        #endregion

        #region Properties

        public int Amount { get; set; }

        /// <summary>
        ///     Permission to handle the packet
        /// </summary>
        public AuthorityType[] Authorities { get; set; }

        /// <summary>
        ///     String identification of the Packet
        /// </summary>
        public string[] Identification { get; set; }

        public bool IsCharScreen { get; set; }

        /// <summary>
        ///     Pass the packet to handler method even if the serialization has failed.
        /// </summary>
        public bool PassNonParseablePacket { get; set; }

        public AuthorityType Authority { get; set; }

        #endregion
    }
}