using NosGm.DAL.DAO;
using NosGm.Data;
using NosGm.Domain;
using System;

namespace NosGm.DAL
{
    public sealed class BazaarListingService
    {
        private static readonly Lazy<BazaarListingService> LazyInstance =
            new Lazy<BazaarListingService>(() => new BazaarListingService(new BazaarListingLiveStateDAO()));

        private readonly BazaarListingLiveStateDAO _dao;

        internal BazaarListingService(BazaarListingLiveStateDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarListingService Instance => LazyInstance.Value;

        public BazaarListingResult Commit(BazaarListingDTO request)
        {
            NormalizeNonEquipmentSerials(request);
            return _dao.Commit(request);
        }

        private static void NormalizeNonEquipmentSerials(BazaarListingDTO request)
        {
            if (request?.SourceBefore == null || request.SourceBefore.Type == InventoryType.Equipment)
            {
                return;
            }

            // EquipmentSerialId protects equipment-specific options. Normal inventory stacks
            // can legitimately inherit or duplicate that legacy value after stack operations,
            // so it must not be used as an identity conflict for Main/Etc listings.
            request.SourceBefore.EquipmentSerialId = Guid.Empty;

            if (request.SourceAfter != null)
            {
                request.SourceAfter.EquipmentSerialId = Guid.Empty;
                return;
            }

            // A full transfer preserves the source ItemInstance identity, therefore both sides
            // of the plan must carry the same normalized serial for validation to remain atomic.
            if (request.BazaarItemAfter != null)
            {
                request.BazaarItemAfter.EquipmentSerialId = Guid.Empty;
            }
        }
    }
}
