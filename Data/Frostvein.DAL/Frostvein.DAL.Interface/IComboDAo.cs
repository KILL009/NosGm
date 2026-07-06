using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IComboDAO
    {
        #region Methods

        ComboDTO Insert(ComboDTO combo);

        void Insert(List<ComboDTO> combos);

        IEnumerable<ComboDTO> LoadAll();

        ComboDTO LoadById(short comboId);

        IEnumerable<ComboDTO> LoadBySkillVnum(short skillVNum);

        IEnumerable<ComboDTO> LoadByVNumHitAndEffect(short skillVNum, short hit, short effect);

        #endregion
    }
}