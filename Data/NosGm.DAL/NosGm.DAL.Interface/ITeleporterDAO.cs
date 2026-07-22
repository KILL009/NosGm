using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface ITeleporterDAO
    {
        #region Methods

        TeleporterDTO Insert(TeleporterDTO teleporter);

        void Insert(IEnumerable<TeleporterDTO> teleporters);

        IEnumerable<TeleporterDTO> LoadAll();

        TeleporterDTO LoadById(short teleporterId);

        IEnumerable<TeleporterDTO> LoadFromNpc(int npcId);

        #endregion
    }
}