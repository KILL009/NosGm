using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
                    var mail = context.Mail.First(i => i.MailId.Equals(mailId));

                    if (mail != null)
                    {
                        context.Mail.Remove(mail);
                        context.SaveChanges();
                    }

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
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var mailId = mail.MailId;
                    var entity = context.Mail.FirstOrDefault(c => c.MailId.Equals(mailId));

                    if (entity == null)
                    {
                        mail = insert(mail, context);
                        return SaveResult.Inserted;
                    }

                    mail.MailId = entity.MailId;
                    mail = update(entity, mail, context);
                    return SaveResult.Updated;
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
                    MailMapper.ToMailDTO(mail, dto);
                    result.Add(dto);
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
                    var dto = new MailDTO();
                    if (MailMapper.ToMailDTO(context.Mail.FirstOrDefault(i => i.MailId.Equals(mailId)), dto))
                        return dto;

                    return null;
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
            //Where(s => s.SenderId == CharacterId && s.IsSenderCopy && MailList.All(m => m.Value.MailId != s.MailId))
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<MailDTO>();
                foreach (var mail in context.Mail.Where(s => s.SenderId == characterId && s.IsSenderCopy).Take(40))
                {
                    var dto = new MailDTO();
                    MailMapper.ToMailDTO(mail, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public async Task<IEnumerable<MailDTO>> LoadSentToCharacterAsync(long characterId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = await context.Mail
                .Where(s => s.ReceiverId == characterId && !s.IsSenderCopy)
                .Take(50)
                .ToListAsync();

                return result.Select(mail =>
                {
                    var dto = new MailDTO();
                    MailMapper.ToMailDTO(mail, dto);
                    return dto;
                });
            }
        }

        private static MailDTO insert(MailDTO mail, FrostveinContext context)
        {
            try
            {
                var entity = new Mail();
                MailMapper.ToMail(mail, entity);
                context.Mail.Add(entity);
                context.SaveChanges();
                if (MailMapper.ToMailDTO(entity, mail)) return mail;

                return null;
            }
            catch (DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        // raise a new exception nesting the current instance as InnerException
                        Logger.Error(
                            new InvalidOperationException($"{validationErrors.Entry.Entity}:{validationError.ErrorMessage}",
                                raise));
                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static MailDTO update(Mail entity, MailDTO respawn, FrostveinContext context)
        {
            if (entity != null)
            {
                MailMapper.ToMail(respawn, entity);
                context.SaveChanges();
            }

            if (MailMapper.ToMailDTO(entity, respawn)) return respawn;

            return null;
        }

        #endregion
    }
}