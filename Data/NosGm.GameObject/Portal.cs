using NosGm.Data;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class Portal : PortalDTO
    {
        #region Methods

        public string GenerateGp()
        {
            return
                $"gp {SourceX} {SourceY} {ServerManager.GetMapInstance(DestinationMapInstanceId)?.Map.MapId ?? 0} {Type} {PortalId} {(IsDisabled ? 1 : 0)}";
        }

        #endregion

        #region Members

        private Guid _destinationMapInstanceId;

        private Guid _sourceMapInstanceId;

        #endregion

        #region Instantiation

        public Portal()
        {
            OnTraversalEvents = new List<EventContainer>();
        }

        public Portal(PortalDTO input)
        {
            OnTraversalEvents = new List<EventContainer>();
            DestinationMapId = input.DestinationMapId;
            DestinationX = input.DestinationX;
            DestinationY = input.DestinationY;
            IsDisabled = input.IsDisabled;
            PortalId = input.PortalId;
            LevelRequired = input.LevelRequired;
            HeroLevelRequired = input.HeroLevelRequired;
            RequiredItem = input.RequiredItem;
            NomeOggetto = input.NomeOggetto;
            SourceMapId = input.SourceMapId;
            SourceX = input.SourceX;
            SourceY = input.SourceY;
            Type = input.Type;
            RequiredClass = input.RequiredClass;
        }

        #endregion

        #region Properties

        public Guid DestinationMapInstanceId
        {
            get
            {
                if (_destinationMapInstanceId == default && DestinationMapId != -1)
                    _destinationMapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(DestinationMapId);
                return _destinationMapInstanceId;
            }
            set => _destinationMapInstanceId = value;
        }

        public List<EventContainer> OnTraversalEvents { get; set; }

        public Guid SourceMapInstanceId
        {
            get
            {
                if (_sourceMapInstanceId == default)
                    _sourceMapInstanceId = ServerManager.GetBaseMapInstanceIdByMapId(SourceMapId);
                return _sourceMapInstanceId;
            }
            set => _sourceMapInstanceId = value;
        }

        #endregion
    }
}