using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class MailMapper
    {
        #region Methods

        public static bool ToMail(MailDTO input, Mail output)
        {
            if (input == null) return false;

            output.AttachmentAmount = input.AttachmentAmount;
            output.AttachmentLevel = input.AttachmentLevel;
            output.AttachmentRarity = input.AttachmentRarity;
            output.AttachmentUpgrade = input.AttachmentUpgrade;
            output.AttachmentDesign = input.AttachmentDesign;
            output.AttachmentVNum = input.AttachmentVNum;
            output.Date = input.Date;
            output.EqPacket = input.EqPacket;
            output.IsOpened = input.IsOpened;
            output.IsSenderCopy = input.IsSenderCopy;
            output.MailId = input.MailId;
            output.Message = input.Message;
            output.ReceiverId = input.ReceiverId;
            output.SenderClass = input.SenderClass;
            output.SenderGender = input.SenderGender;
            output.SenderHairColor = input.SenderHairColor;
            output.SenderHairStyle = input.SenderHairStyle;
            output.SenderId = input.SenderId;
            output.SenderMorphId = input.SenderMorphId;
            output.Title = input.Title;

            return true;
        }

        public static bool ToMailDTO(Mail input, MailDTO output)
        {
            if (input == null) return false;

            output.AttachmentAmount = input.AttachmentAmount;
            output.AttachmentLevel = input.AttachmentLevel;
            output.AttachmentRarity = input.AttachmentRarity;
            output.AttachmentUpgrade = input.AttachmentUpgrade;
            output.AttachmentDesign = input.AttachmentDesign;
            output.AttachmentVNum = input.AttachmentVNum;
            output.Date = input.Date;
            output.EqPacket = input.EqPacket;
            output.IsOpened = input.IsOpened;
            output.IsSenderCopy = input.IsSenderCopy;
            output.MailId = input.MailId;
            output.Message = input.Message;
            output.ReceiverId = input.ReceiverId;
            output.SenderClass = input.SenderClass;
            output.SenderGender = input.SenderGender;
            output.SenderHairColor = input.SenderHairColor;
            output.SenderHairStyle = input.SenderHairStyle;
            output.SenderId = input.SenderId;
            output.SenderMorphId = input.SenderMorphId;
            output.Title = input.Title;

            return true;
        }

        #endregion
    }
}