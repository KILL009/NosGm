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
    public class NpcMonsterDAO : INpcMonsterDAO
    {
        private static readonly ICacheService<short, NpcMonsterDTO> _cache = new NosGm.DAL.EF.Cache.MemoryCacheService<short, NpcMonsterDTO>(dto => dto.Clone());
        private static int _isFullyLoaded;
        private static readonly object _loadLock = new object();

        #region Methods

        public IEnumerable<NpcMonsterDTO> FindByName(string name)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<NpcMonsterDTO>();
                foreach (var npcMonster in context.NpcMonster.Where(s =>
                    string.IsNullOrEmpty(name) ? s.Name.Equals("") : s.Name.Contains(name)))
                {
                    var dto = new NpcMonsterDTO();
                    NpcMonsterMapper.ToNpcMonsterDTO(npcMonster, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public void Insert(List<NpcMonsterDTO> npcMonsters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Item in npcMonsters)
                    {
                        var entity = new NpcMonster();
                        NpcMonsterMapper.ToNpcMonster(Item, entity);
                        context.NpcMonster.Add(entity);
                    }

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

        public NpcMonsterDTO Insert(NpcMonsterDTO npc)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new NpcMonster();
                    NpcMonsterMapper.ToNpcMonster(npc, entity);
                    context.NpcMonster.Add(entity);
                    context.SaveChanges();
                    if (NpcMonsterMapper.ToNpcMonsterDTO(entity, npc))
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(npc.NpcMonsterVNum, npc);
                        }
                        return npc;
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

        public SaveResult InsertOrUpdate(ref NpcMonsterDTO npcMonster)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var npcMonsterVNum = npcMonster.NpcMonsterVNum;
                    var entity = context.NpcMonster.FirstOrDefault(c => c.NpcMonsterVNum.Equals(npcMonsterVNum));

                    if (entity == null)
                    {
                        npcMonster = insert(npcMonster, context);
                        if (npcMonster != null)
                        {
                            lock (_loadLock)
                            {
                                _cache.Set(npcMonster.NpcMonsterVNum, npcMonster);
                            }
                        }
                        return SaveResult.Inserted;
                    }

                    npcMonster = update(entity, npcMonster, context);
                    if (npcMonster != null)
                    {
                        lock (_loadLock)
                        {
                            _cache.Set(npcMonster.NpcMonsterVNum, npcMonster);
                        }
                    }
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    string.Format(Language.Instance.GetMessageFromKey("UPDATE_NPCMONSTER_ERROR"),
                        npcMonster.NpcMonsterVNum, e.Message), e);
                return SaveResult.Error;
            }
        }

        public IEnumerable<NpcMonsterDTO> LoadAll()
        {
            lock (_loadLock)
            {
                if (Volatile.Read(ref _isFullyLoaded) == 1)
                {
                    return _cache.GetAll().ToList();
                }

                using (var context = DataAccessHelper.CreateContext())
                {
                    var result = new List<NpcMonsterDTO>();
                    var cacheItems = new List<KeyValuePair<short, NpcMonsterDTO>>();
                    foreach (var NpcMonster in context.NpcMonster.AsNoTracking())
                    {
                        var dto = new NpcMonsterDTO();
                        NpcMonsterMapper.ToNpcMonsterDTO(NpcMonster, dto);
                        cacheItems.Add(new KeyValuePair<short, NpcMonsterDTO>(dto.NpcMonsterVNum, dto));
                        result.Add(dto);
                    }

                    _cache.ReplaceAll(cacheItems);
                    Volatile.Write(ref _isFullyLoaded, 1);
                    return result;
                }
            }
        }

        public CacheStatisticsSnapshot GetCacheStatistics() => _cache.GetStatistics();

        public NpcMonsterDTO LoadByVNum(short npcMonsterVNum)
        {
            try
            {
                if (_cache.TryGetValue(npcMonsterVNum, out var cachedDto))
                {
                    return cachedDto;
                }

                lock (_loadLock)
                {
                    if (_cache.TryGetValue(npcMonsterVNum, out cachedDto))
                    {
                        return cachedDto;
                    }

                    using (var context = DataAccessHelper.CreateContext())
                    {
                        var dto = new NpcMonsterDTO();
                        if (NpcMonsterMapper.ToNpcMonsterDTO(
                            context.NpcMonster.AsNoTracking().FirstOrDefault(i => i.NpcMonsterVNum.Equals(npcMonsterVNum)), dto))
                        {
                            _cache.Set(npcMonsterVNum, dto);
                            return dto;
                        }

                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private static NpcMonsterDTO insert(NpcMonsterDTO npcMonster, NosGmContext context)
        {
            var entity = new NpcMonster();
            NpcMonsterMapper.ToNpcMonster(npcMonster, entity);
            context.NpcMonster.Add(entity);
            context.SaveChanges();
            if (NpcMonsterMapper.ToNpcMonsterDTO(entity, npcMonster)) return npcMonster;

            return null;
        }

        private static NpcMonsterDTO update(NpcMonster entity, NpcMonsterDTO npcMonster, NosGmContext context)
        {
            if (entity != null)
            {
                NpcMonsterMapper.ToNpcMonster(npcMonster, entity);
                context.SaveChanges();
            }

            if (NpcMonsterMapper.ToNpcMonsterDTO(entity, npcMonster)) return npcMonster;

            return null;
        }

        #endregion
    }
}