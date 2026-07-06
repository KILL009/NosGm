using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IPartnerSkillDAO
    {
        #region Methods

        PartnerSkillDTO Insert(PartnerSkillDTO partnerSkillDTO);

        List<PartnerSkillDTO> LoadByEquipmentSerialId(Guid equipmentSerialId);

        DeleteResult Remove(long partnerSkillId);

        #endregion
    }
}