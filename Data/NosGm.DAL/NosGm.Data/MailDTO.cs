using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public class MailDTO
    {
        #region Properties

        public short AttachmentAmount { get; set; }

        public short AttachmentDesign { get; set; }

        public byte AttachmentLevel { get; set; }

        public byte AttachmentRarity { get; set; }

        public byte AttachmentUpgrade { get; set; }

        public short? AttachmentVNum { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// Stable business-operation identifier used to make mail and reward delivery idempotent.
        /// It is persisted in MailDeliveryOperation instead of the legacy Mail table.
        /// </summary>
        public Guid? DeliveryOperationId { get; set; }

        /// <summary>
        /// Subsystem that created the delivery. Used by item trace events when the parcel is claimed.
        /// </summary>
        public ItemTraceSource DeliverySource { get; set; }

        public string EqPacket { get; set; }

        public bool IsOpened { get; set; }

        public bool IsSenderCopy { get; set; }

        public long MailId { get; set; }

        public string Message { get; set; }

        public long ReceiverId { get; set; }

        public ClassType SenderClass { get; set; }

        public GenderType SenderGender { get; set; }

        public HairColorType SenderHairColor { get; set; }

        public HairStyleType SenderHairStyle { get; set; }

        public long SenderId { get; set; }

        public short SenderMorphId { get; set; }

        public string Title { get; set; }

        #endregion
    }
}
