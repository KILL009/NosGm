using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Domain;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.DAL.DAO
{
    public class MailDAO : IMailDAO
    {
        #region Methods

        public DeleteResult DeleteById(long mailId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mail = context.Mail.FirstOrDefault(i => i.MailId.Equals(mailId));
                    if (mail == null)
                    {
                        return DeleteResult.NotFound;
                    }

                    context.Mail.Remove(mail);
                    context.SaveChanges();
                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref MailDTO mail)
        {
            if (mail == null)
            {
                return SaveResult.Error;
            }

            if (mail.MailId == 0 && mail.DeliveryOperationId.HasValue && mail.DeliveryOperationId.Value != Guid.Empty)
            {
                var idempotentResult = InsertOrGetIdempotent(ref mail);
                if (idempotentResult.HasValue)
                {
                    return idempotentResult.Value;
                }
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mailId = mail.MailId;
                    var entity = context.Mail.FirstOrDefault(c => c.MailId.Equals(mailId));

                    if (entity == null)
                    {
                        var inserted = Insert(mail, context);
                        if (inserted == null)
                        {
                            return SaveResult.Error;
                        }

                        mail = inserted;
                        return SaveResult.Inserted;
                    }

                    mail.MailId = entity.MailId;
                    mail = Update(entity, mail, context);
                    return mail == null ? SaveResult.Error : SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<MailDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MailDTO>();
                foreach (var mail in context.Mail)
                {
                    var dto = new MailDTO();
                    if (MailMapper.ToMailDTO(mail, dto))
                    {
                        EnrichDeliveryMetadata(context, dto);
                        result.Add(dto);
                    }
                }

                return result;
            }
        }

        public MailDTO LoadById(long mailId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return LoadById(context, mailId);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<MailDTO> LoadSentByCharacter(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MailDTO>();
                foreach (var mail in context.Mail.Where(s => s.SenderId == characterId && s.IsSenderCopy).Take(40))
                {
                    var dto = new MailDTO();
                    if (MailMapper.ToMailDTO(mail, dto))
                    {
                        EnrichDeliveryMetadata(context, dto);
                        result.Add(dto);
                    }
                }

                return result;
            }
        }

        public async Task<IEnumerable<MailDTO>> LoadSentToCharacterAsync(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var entities = await context.Mail
                    .Where(s => s.ReceiverId == characterId && !s.IsSenderCopy)
                    .Take(50)
                    .ToListAsync();

                var result = new List<MailDTO>();
                foreach (var entity in entities)
                {
                    var dto = new MailDTO();
                    if (MailMapper.ToMailDTO(entity, dto))
                    {
                        EnrichDeliveryMetadata(context, dto);
                        result.Add(dto);
                    }
                }

                return result;
            }
        }

        public void MarkDeliveryClaimed(long mailId, Guid itemInstanceId)
        {
            if (mailId <= 0 || itemInstanceId == Guid.Empty)
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID(N'dbo.MailDeliveryOperation', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.MailDeliveryOperation
       SET ClaimedAtUtc = COALESCE(ClaimedAtUtc, SYSUTCDATETIME()),
           ClaimItemInstanceId = COALESCE(ClaimItemInstanceId, @ItemInstanceId)
     WHERE MailId = @MailId;
END";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Database.ExecuteSqlCommand(sql,
                        new SqlParameter("@MailId", SqlDbType.BigInt) { Value = mailId },
                        new SqlParameter("@ItemInstanceId", SqlDbType.UniqueIdentifier) { Value = itemInstanceId });
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to mark mail delivery as claimed.", exception);
            }
        }

        private static SaveResult? InsertOrGetIdempotent(ref MailDTO mail)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.MailDeliveryOperation', N'U') IS NULL
BEGIN
    SELECT CAST(-1 AS BIGINT);
    RETURN;
END;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @MailId BIGINT;
SELECT @MailId = MailId
  FROM dbo.MailDeliveryOperation WITH (UPDLOCK, HOLDLOCK)
 WHERE OperationId = @OperationId
   AND IsSenderCopy = @IsSenderCopy;

IF @MailId IS NULL
   AND NOT EXISTS
       (SELECT 1
          FROM dbo.MailDeliveryOperation WITH (UPDLOCK, HOLDLOCK)
         WHERE OperationId = @OperationId
           AND IsSenderCopy = @IsSenderCopy)
BEGIN
    INSERT INTO dbo.Mail
    (AttachmentAmount, AttachmentDesign, AttachmentLevel, AttachmentRarity,
     AttachmentUpgrade, AttachmentVNum, [Date], EqPacket, IsOpened,
     IsSenderCopy, [Message], ReceiverId, SenderClass, SenderGender,
     SenderHairColor, SenderHairStyle, SenderId, SenderMorphId, Title)
    VALUES
    (@AttachmentAmount, @AttachmentDesign, @AttachmentLevel, @AttachmentRarity,
     @AttachmentUpgrade, @AttachmentVNum, @Date, @EqPacket, @IsOpened,
     @IsSenderCopy, @Message, @ReceiverId, @SenderClass, @SenderGender,
     @SenderHairColor, @SenderHairStyle, @SenderId, @SenderMorphId, @Title);

    SET @MailId = CONVERT(BIGINT, SCOPE_IDENTITY());

    INSERT INTO dbo.MailDeliveryOperation
    (OperationId, IsSenderCopy, MailId, DeliverySource, ReceiverId, CreatedAtUtc)
    VALUES
    (@OperationId, @IsSenderCopy, @MailId, @DeliverySource, @ReceiverId, SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
SELECT ISNULL(@MailId, CAST(0 AS BIGINT));";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mailId = context.Database.SqlQuery<long>(sql, BuildIdempotentParameters(mail)).Single();
                    if (mailId == -1)
                    {
                        return null;
                    }

                    if (mailId <= 0)
                    {
                        Logger.Error("Idempotent mail delivery returned no MailId.");
                        return SaveResult.Error;
                    }

                    var persisted = LoadById(context, mailId);
                    if (persisted != null)
                    {
                        mail = persisted;
                    }
                    else
                    {
                        // The same operation was already completed and its mail row was consumed.
                        // Keep the operation's MailId so callers can suppress a second notification.
                        mail.MailId = mailId;
                    }

                    return SaveResult.Updated;
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to insert idempotent mail delivery.", exception);
                return SaveResult.Error;
            }
        }

        private static object[] BuildIdempotentParameters(MailDTO mail)
        {
            return new object[]
            {
                Parameter("@OperationId", SqlDbType.UniqueIdentifier, mail.DeliveryOperationId.Value),
                Parameter("@DeliverySource", SqlDbType.Int, (int)mail.DeliverySource),
                Parameter("@AttachmentAmount", SqlDbType.SmallInt, mail.AttachmentAmount),
                Parameter("@AttachmentDesign", SqlDbType.SmallInt, mail.AttachmentDesign),
                Parameter("@AttachmentLevel", SqlDbType.TinyInt, mail.AttachmentLevel),
                Parameter("@AttachmentRarity", SqlDbType.TinyInt, mail.AttachmentRarity),
                Parameter("@AttachmentUpgrade", SqlDbType.TinyInt, mail.AttachmentUpgrade),
                Parameter("@AttachmentVNum", SqlDbType.SmallInt, mail.AttachmentVNum),
                Parameter("@Date", SqlDbType.DateTime, mail.Date == default(DateTime) ? DateTime.Now : mail.Date),
                Parameter("@EqPacket", SqlDbType.NVarChar, mail.EqPacket),
                Parameter("@IsOpened", SqlDbType.Bit, mail.IsOpened),
                Parameter("@IsSenderCopy", SqlDbType.Bit, mail.IsSenderCopy),
                Parameter("@Message", SqlDbType.NVarChar, mail.Message),
                Parameter("@ReceiverId", SqlDbType.BigInt, mail.ReceiverId),
                Parameter("@SenderClass", SqlDbType.Int, (int)mail.SenderClass),
                Parameter("@SenderGender", SqlDbType.Int, (int)mail.SenderGender),
                Parameter("@SenderHairColor", SqlDbType.Int, (int)mail.SenderHairColor),
                Parameter("@SenderHairStyle", SqlDbType.Int, (int)mail.SenderHairStyle),
                Parameter("@SenderId", SqlDbType.BigInt, mail.SenderId),
                Parameter("@SenderMorphId", SqlDbType.SmallInt, mail.SenderMorphId),
                Parameter("@Title", SqlDbType.NVarChar, mail.Title)
            };
        }

        private static SqlParameter Parameter(string name, SqlDbType type, object value)
        {
            var parameter = new SqlParameter(name, type)
            {
                Value = value ?? DBNull.Value
            };
            return parameter;
        }

        private static MailDTO LoadById(FrostveinContext context, long mailId)
        {
            var entity = context.Mail.FirstOrDefault(i => i.MailId.Equals(mailId));
            if (entity == null)
            {
                return null;
            }

            var dto = new MailDTO();
            if (!MailMapper.ToMailDTO(entity, dto))
            {
                return null;
            }

            EnrichDeliveryMetadata(context, dto);
            return dto;
        }

        private static void EnrichDeliveryMetadata(FrostveinContext context, MailDTO mail)
        {
            if (context == null || mail == null || mail.MailId <= 0)
            {
                return;
            }

            const string sql = @"
IF OBJECT_ID(N'dbo.MailDeliveryOperation', N'U') IS NOT NULL
BEGIN
    SELECT TOP (1) OperationId, DeliverySource
      FROM dbo.MailDeliveryOperation
     WHERE MailId = @MailId;
END";

            try
            {
                var metadata = context.Database.SqlQuery<DeliveryMetadataRow>(sql,
                    new SqlParameter("@MailId", SqlDbType.BigInt) { Value = mail.MailId }).FirstOrDefault();
                if (metadata == null)
                {
                    return;
                }

                mail.DeliveryOperationId = metadata.OperationId;
                mail.DeliverySource = (ItemTraceSource)metadata.DeliverySource;
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to load mail delivery metadata.", exception);
            }
        }

        private static MailDTO Insert(MailDTO mail, FrostveinContext context)
        {
            try
            {
                var entity = new Mail();
                MailMapper.ToMail(mail, entity);
                context.Mail.Add(entity);
                context.SaveChanges();
                return MailMapper.ToMailDTO(entity, mail) ? mail : null;
            }
            catch (DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Logger.Error(new InvalidOperationException(
                            $"{validationErrors.Entry.Entity}:{validationError.ErrorMessage}", raise));
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static MailDTO Update(Mail entity, MailDTO mail, FrostveinContext context)
        {
            if (entity != null)
            {
                MailMapper.ToMail(mail, entity);
                context.SaveChanges();
            }

            return MailMapper.ToMailDTO(entity, mail) ? mail : null;
        }

        private sealed class DeliveryMetadataRow
        {
            public Guid OperationId { get; set; }

            public int DeliverySource { get; set; }
        }

        #endregion
    }
}
