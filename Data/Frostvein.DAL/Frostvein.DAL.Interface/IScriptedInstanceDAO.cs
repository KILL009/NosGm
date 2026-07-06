using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IScriptedInstanceDAO
    {
        #region Methods

        ScriptedInstanceDTO Insert(ScriptedInstanceDTO scriptedInstance);

        void Insert(List<ScriptedInstanceDTO> scriptedInstances);

        IEnumerable<ScriptedInstanceDTO> LoadByMap(short mapId);

        #endregion
    }
}