using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class CellonOptionDAO : ICellonOptionDAO
    {
        #region Methods

        public DeleteResult DeleteByEquipmentSerialId(Guid id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var deleteentities = context.CellonOption.Where(s => s.EquipmentSerialId == id).ToList();
                    if (deleteentities.Count != 0)
                    {
                        context.CellonOption.RemoveRange(deleteentities);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format(Language.Instance.GetMessageFromKey("DELETE_ERROR"), id, e.Message), e);
                return DeleteResult.Error;
            }
        }

        public IEnumerable<CellonOptionDTO> GetOptionsByWearableInstanceId(Guid wearableInstanceId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<CellonOptionDTO>();
                foreach (var entity in context.CellonOption.Where(c => c.EquipmentSerialId == wearableInstanceId))
                {
                    var dto = new CellonOptionDTO();
                    CellonOptionMapper.ToCellonOptionDTO(entity, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public CellonOptionDTO InsertOrUpdate(CellonOptionDTO cellonOption)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var cellonOptionId = cellonOption.CellonOptionId;
                    var entity = context.CellonOption.FirstOrDefault(c => c.CellonOptionId.Equals(cellonOptionId));

                    if (entity == null)
                    {
                        return insert(cellonOption, context);
                    }

                    return update(entity, cellonOption, context);
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("INSERT_ERROR"), cellonOption, e.Message), e);
                return cellonOption;
            }
        }

        public void InsertOrUpdateFromList(List<CellonOptionDTO> cellonOption, Guid equipmentSerialId)
        {
            try
            {
                if (!cellonOption.Any()) return;

                using (var context = DataAccessHelper.CreateContext())
                {
                    void insert(CellonOptionDTO cellonoption)
                    {
                        var _entity = new CellonOption();
                        CellonOptionMapper.ToCellonOption(cellonoption, _entity);
                        context.CellonOption.Add(_entity);
                        context.SaveChanges();
                        cellonoption.CellonOptionId = _entity.CellonOptionId;
                    }

                    void update(CellonOption _entity, CellonOptionDTO cellonoption)
                    {
                        if (_entity != null)
                        {
                            CellonOptionMapper.ToCellonOption(cellonoption, _entity);
                        }
                    }

                    foreach (var item in cellonOption)
                    {
                        item.EquipmentSerialId = equipmentSerialId;
                        var entity = context.CellonOption.FirstOrDefault(c => c.CellonOptionId == item.CellonOptionId);

                        if (entity == null)
                        {
                            insert(item);
                        }
                        else
                        {
                            update(entity, item);
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static CellonOptionDTO insert(CellonOptionDTO cellonOption, NosGmContext context)
        {
            var entity = new CellonOption();
            CellonOptionMapper.ToCellonOption(cellonOption, entity);
            context.CellonOption.Add(entity);
            context.SaveChanges();
            if (CellonOptionMapper.ToCellonOptionDTO(entity, cellonOption))
            {
                return cellonOption;
            }

            return null;
        }

        private static CellonOptionDTO update(CellonOption entity, CellonOptionDTO cellonOption, NosGmContext context)
        {
            if (entity != null)
            {
                CellonOptionMapper.ToCellonOption(cellonOption, entity);
                context.SaveChanges();
            }

            if (CellonOptionMapper.ToCellonOptionDTO(entity, cellonOption))
            {
                return cellonOption;
            }

            return null;
        }

        public async Task InsertOrUpdateFromListAsync(List<CellonOptionDTO> cellonOption, Guid equipmentSerialId)
        {
            try
            {
                if (!cellonOption.Any()) return;

                using (var context = DataAccessHelper.CreateContext())
                {
                    async Task InsertAsync(CellonOptionDTO cellonoption)
                    {
                        var _entity = new CellonOption();
                        CellonOptionMapper.ToCellonOption(cellonoption, _entity);
                        context.CellonOption.Add(_entity);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        cellonoption.CellonOptionId = _entity.CellonOptionId;
                    }

                    async Task UpdateAsync(CellonOption _entity, CellonOptionDTO cellonoption)
                    {
                        if (_entity != null)
                        {
                            CellonOptionMapper.ToCellonOption(cellonoption, _entity);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }

                    foreach (var item in cellonOption)
                    {
                        item.EquipmentSerialId = equipmentSerialId;
                        var entity = context.CellonOption.FirstOrDefault(c => c.CellonOptionId == item.CellonOptionId);

                        if (entity == null)
                        {
                            await InsertAsync(item).ConfigureAwait(false);
                        }
                        else
                        {
                            await UpdateAsync(entity, item).ConfigureAwait(false);
                        }
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #endregion
    }
}