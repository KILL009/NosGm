using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Frostvein.DAL.EF
{
    public abstract class SynchronizableBaseEntity
    {
        #region Properties

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        #endregion
    }
}