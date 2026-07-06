using System;

namespace Frostvein.Data
{
    [Serializable]
    public abstract class SynchronizableBaseDTO
    {
        #region Instantiation

        protected SynchronizableBaseDTO() => Id = Guid.NewGuid();

        #endregion

        // make unique

        #region Properties

        public Guid Id { get; set; }

        #endregion

        #region Methods

        public override bool Equals(object obj) => ((SynchronizableBaseDTO)obj).Id == Id;

        public override int GetHashCode() => Id.GetHashCode();

        #endregion
    }
}