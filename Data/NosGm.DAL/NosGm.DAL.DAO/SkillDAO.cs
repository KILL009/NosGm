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
using System.Threading;

namespace NosGm.DAL.DAO
{
    public class SkillDAO : ISkillDAO
    {
        private static readonly ICacheService<short, SkillDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, SkillDTO>(dto => dto.Clone());
        private static int _isFullyLoaded;
        private static readonly object _loadLock = new object();

        #region Methods

        public void Insert(List<SkillDTO> skills)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var skill in skills) InsertOrUpdate(skill);
                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }

                lock (_loadLock)
                {
                    Volatile.Write(ref _isFullyLoaded, 0);
                    _cache.Clear();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public SkillDTO Insert(SkillDTO skill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Skill();
                    SkillMapper.ToSkill(skill, entity);
                    context.Skill.Add(entity);
                    context.SaveChanges();
                    if (SkillMapper.ToSkillDTO(entity, skill))
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(skill.SkillVNum, skill);
                        }
                        return skill;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public SaveResult InsertOrUpdate(SkillDTO skill)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    long SkillVNum = skill.SkillVNum;
                    var entity = context.Skill.FirstOrDefault(c => c.SkillVNum == SkillVNum);

                    if (entity == null)
                    {
                        skill = insert(skill, context);
                        if (skill != null)
                        {
                            lock (_loadLock)
                            {
                                _cache.Set(skill.SkillVNum, skill);
                            }
                        }
                        return SaveResult.Inserted;
                    }

                    skill = update(entity, skill, context);
                    if (skill != null)
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(skill.SkillVNum, skill);
                        }
                    }
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_SKILL_ERROR"), skill.SkillVNum,
                        e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<SkillDTO> LoadAll()
        {
            if (Volatile.Read(ref _isFullyLoaded) == 1)
            {
                return _cache.GetAll();
            }

            lock (_loadLock)
            {
                if (Volatile.Read(ref _isFullyLoaded) == 1)
                {
                    return _cache.GetAll();
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = new List<SkillDTO>();
                    var cacheItems = new List<KeyValuePair<short, SkillDTO>>();
                    foreach (var Skill in context.Skill.AsNoTracking())
                    {
                        var dto = new SkillDTO();
                        SkillMapper.ToSkillDTO(Skill, dto);
                        cacheItems.Add(new KeyValuePair<short, SkillDTO>(dto.SkillVNum, dto));
                        result.Add(dto);
                    }

                    _cache.ReplaceAll(cacheItems);
                    Volatile.Write(ref _isFullyLoaded, 1);
                    return result;
                }
            }
        }

        public CacheStatisticsSnapshot GetCacheStatistics() => _cache.GetStatistics();

        public SkillDTO LoadById(short skillId)
        {
            try
            {
                if (_cache.TryGetValue(skillId, out var cachedDto))
                {
                    return cachedDto;
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var dto = new SkillDTO();
                    if (SkillMapper.ToSkillDTO(context.Skill.AsNoTracking().FirstOrDefault(s => s.SkillVNum.Equals(skillId)), dto))
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(skillId, dto);
                        }
                        return dto;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static SkillDTO insert(SkillDTO skill, NosGmContext context)
        {
            var entity = new Skill();
            SkillMapper.ToSkill(skill, entity);
            context.Skill.Add(entity);
            context.SaveChanges();
            if (SkillMapper.ToSkillDTO(entity, skill)) return skill;

            return null;
        }

        private static SkillDTO update(Skill entity, SkillDTO skill, NosGmContext context)
        {
            if (entity != null)
            {
                SkillMapper.ToSkill(skill, entity);
                context.SaveChanges();
            }

            if (SkillMapper.ToSkillDTO(entity, skill)) return skill;

            return null;
        }

        #endregion
    }
}