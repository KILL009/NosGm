using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IBCardDAO
    {
        #region Methods

        DeleteResult DeleteByCardId(short cardId);

        DeleteResult DeleteByItemVNum(short itemVNum);

        DeleteResult DeleteByMonsterVNum(short monsterVNum);

        DeleteResult DeleteBySkillVNum(short skillVNum);

        BCardDTO Insert(ref BCardDTO cardObject);

        void Insert(List<BCardDTO> cards);

        IEnumerable<BCardDTO> LoadAll();

        IEnumerable<BCardDTO> LoadByCardId(short cardId);

        BCardDTO LoadById(short cardId);

        IEnumerable<BCardDTO> LoadByItemVNum(short vNum);

        IEnumerable<BCardDTO> LoadByNpcMonsterVNum(short vNum);

        IEnumerable<BCardDTO> LoadBySkillVNum(short vNum);

        #endregion
    }
}