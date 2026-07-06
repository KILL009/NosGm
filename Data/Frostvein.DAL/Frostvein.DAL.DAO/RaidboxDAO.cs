using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public class RaidboxDAO : IRaidboxDAO
    {
        #region Methods

        public RaidboxDTO Insert(RaidboxDTO item)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Raidbox();
                    RaidboxMapper.ToRaidbox(item, entity);
                    context.Raidbox.Add(entity);
                    context.SaveChanges();
                    if (RaidboxMapper.ToRaidboxDTO(entity, item)) return item;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<RaidboxDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RaidboxDTO>();
                foreach (var item in context.Raidbox)
                {
                    var dto = new RaidboxDTO();
                    RaidboxMapper.ToRaidboxDTO(item, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public RaidboxDTO LoadById(short id)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new RaidboxDTO();
                    if (RaidboxMapper.ToRaidboxDTO(
                        context.Raidbox.FirstOrDefault(i => i.RaidboxId.Equals(id)), dto))
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

        public IEnumerable<RaidboxDTO> LoadByItemVNum(short vnum)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<RaidboxDTO>();
                foreach (var item in context.Raidbox.Where(s => s.OriginalItemVNum == vnum))
                {
                    var dto = new RaidboxDTO();
                    RaidboxMapper.ToRaidboxDTO(item, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<RaidboxDTO> LoadByItemVNumAndDesign(short vnum, short design)
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<RaidboxDTO> result = new List<RaidboxDTO>();
                foreach (Raidbox item in context.Raidbox.Where(s => s.OriginalItemVNum == vnum && s.OriginalItemDesign == design))
                {
                    RaidboxDTO dto = new RaidboxDTO();
                    Mapper.Mappers.RaidboxMapper.ToRaidboxDTO(item, dto);
                    result.Add(dto);
                }
                return result;
            }
        }

        #endregion
    }
}