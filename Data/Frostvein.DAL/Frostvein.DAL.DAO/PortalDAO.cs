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
    public class PortalDAO : IPortalDAO
    {
        #region Methods

        public void Insert(List<PortalDTO> portals)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Item in portals)
                    {
                        var entity = new Portal();
                        PortalMapper.ToPortal(Item, entity);
                        context.Portal.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public PortalDTO Insert(PortalDTO portal)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var entity = new Portal();
                    PortalMapper.ToPortal(portal, entity);
                    context.Portal.Add(entity);
                    context.SaveChanges();
                    if (PortalMapper.ToPortalDTO(entity, portal)) return portal;

                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public IEnumerable<PortalDTO> LoadByMap(short mapId)
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<PortalDTO>();
                foreach (var Portalobject in context.Portal.Where(c => c.SourceMapId.Equals(mapId)))
                {
                    var dto = new PortalDTO();
                    PortalMapper.ToPortalDTO(Portalobject, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        public IEnumerable<PortalDTO> LoadAll()
        {
            using (var context = DataAccessHelper.CreateContext())
            {
                var result = new List<PortalDTO>();
                foreach (var Map in context.Portal)
                {
                    var dto = new PortalDTO();
                    PortalMapper.ToPortalDTO(Map, dto);
                    result.Add(dto);
                }

                return result;
            }
        }

        #endregion
    }
}