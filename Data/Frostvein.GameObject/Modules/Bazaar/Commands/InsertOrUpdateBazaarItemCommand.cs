using FluentValidation;
using MediatR;
using Frostvein.Data;

namespace Frostvein.GameObject.Modules.Bazaar.Commands
{
    public class InsertOrUpdateBazaarItemCommand : IRequest<long>
    {
        public BazaarItemDTO BazaarItem { get; set; }

        /// <summary>
        /// Reload the listing and its remaining ItemInstance from the database without
        /// writing the caller snapshot back. Purchases use this after their SQL commit
        /// so an overlapping recollection can never be resurrected by a stale cache write.
        /// </summary>
        public bool RefreshOnly { get; set; }
    }

    public class InsertOrUpdateBazaarItemCommandValidator : AbstractValidator<InsertOrUpdateBazaarItemCommand>
    {
        public InsertOrUpdateBazaarItemCommandValidator()
        {
            RuleFor(m => m.BazaarItem).NotNull();
        }
    }
}
