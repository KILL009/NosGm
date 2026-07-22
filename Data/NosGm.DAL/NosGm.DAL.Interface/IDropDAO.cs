using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IDropDAO
    {
        #region Methods

        DropDTO Insert(DropDTO drop);

        void Insert(List<DropDTO> drops);

        List<DropDTO> LoadAll();

        IEnumerable<DropDTO> LoadByMonster(short monsterVNum);

        IEnumerable<DropDTO> LoadByMapOrMonsters(short mapTypeId, List<short> monsterVNums);

        #endregion
    }
}