using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IMapMonsterDAO
    {
        #region Methods

        DeleteResult DeleteById(int mapMonsterId);

        bool DoesMonsterExist(int mapMonsterId);

        MapMonsterDTO Insert(MapMonsterDTO mapMonster);

        void Insert(IEnumerable<MapMonsterDTO> mapMonsters);

        MapMonsterDTO LoadById(int mapMonsterId);

        IEnumerable<MapMonsterDTO> LoadFromMap(short mapId);

        SaveResult Update(ref MapMonsterDTO mapMonster);

        IEnumerable<MapMonsterDTO> LoadAll();

        #endregion
    }
}