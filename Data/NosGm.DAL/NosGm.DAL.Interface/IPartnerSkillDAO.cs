using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
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