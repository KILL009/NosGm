using FluentValidation;
using MediatR;
using NosGm.Data;

namespace NosGm.GameObject.Modules.Bazaar.Commands
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

    /// <summary>
    /// Sends the complete server-side listing plan to the dedicated NosBazaar service.
    /// The service owns the SQL transaction and cache refresh; the World applies the
    /// returned plan to live memory only after a successful response.
    /// </summary>
    public sealed class CommitBazaarListingCommand : IRequest<BazaarListingCommitResponseDTO>
    {
        public BazaarListingDTO Plan { get; set; }
    }

    public sealed class CommitBazaarListingCommandValidator : AbstractValidator<CommitBazaarListingCommand>
    {
        public CommitBazaarListingCommandValidator()
        {
            RuleFor(command => command).NotNull();
            RuleFor(command => command.Plan).NotNull();
            RuleFor(command => command.Plan.OperationId).NotEmpty();
            RuleFor(command => command.Plan.SellerAccountId).GreaterThan(0);
            RuleFor(command => command.Plan.SellerCharacterId).GreaterThan(0);
            RuleFor(command => command.Plan.SourceBefore).NotNull();
            RuleFor(command => command.Plan.BazaarItemAfter).NotNull();
            RuleFor(command => command.Plan.Listing).NotNull();
        }
    }
}
