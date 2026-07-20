using FluentValidation;
using MediatR;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Commands.InsertOrUpdateBazaarItem
{
    /// <summary>
    /// Authoritative listing entry point. SQL and the NosBazaar cache are owned by this
    /// service; the World server receives the committed plan and only then mutates memory.
    /// </summary>
    internal sealed class CommitBazaarListingCommandHandler
        : IRequestHandler<CommitBazaarListingCommand, BazaarListingCommitResponseDTO>
    {
        private readonly CommitBazaarListingCommandValidator _commandValidator = new();
        private readonly BazaarManager _bazaarManager;

        public CommitBazaarListingCommandHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager ?? throw new ArgumentNullException(nameof(bazaarManager));
        }

        public async Task<BazaarListingCommitResponseDTO> Handle(
            CommitBazaarListingCommand command,
            CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command, cancellationToken);

            BazaarListingDTO plan = command.Plan;
            if (_bazaarManager.TryGetCommitResponse(plan.OperationId, out BazaarListingCommitResponseDTO cached))
            {
                return cached;
            }

            BazaarListingResult result = BazaarListingService.Instance.Commit(plan);
            var response = new BazaarListingCommitResponseDTO
            {
                Result = result,
                Plan = plan,
                CacheRefreshed = false
            };

            if (result != BazaarListingResult.Success &&
                result != BazaarListingResult.AlreadyCommitted)
            {
                response.Message = $"Listing transaction was rejected with {result}.";
                return response;
            }

            // A duplicate after the short response cache expired means SQL contains the
            // operation but this process no longer has the exact committed before/after plan.
            // Do not return an incomplete plan to the World. A relog will load the durable state.
            if (result == BazaarListingResult.AlreadyCommitted)
            {
                response.Result = BazaarListingResult.StateChanged;
                response.Message =
                    "The listing was already committed, but its live response expired. Relog to recover the durable inventory state.";
                long existingListingId = plan?.Listing?.BazaarItemId ?? 0;
                _bazaarManager.TryRefreshListing(existingListingId, out _);
                return response;
            }

            long bazaarItemId = plan?.Listing?.BazaarItemId ?? 0;
            string failure = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (_bazaarManager.TryRefreshListing(bazaarItemId, out failure))
                {
                    response.CacheRefreshed = true;
                    response.Message = "Listing committed and cached by the NosBazaar service.";
                    _bazaarManager.RememberCommitResponse(plan.OperationId, response);
                    return response;
                }

                if (attempt < 3)
                {
                    await Task.Delay(50 * attempt, cancellationToken);
                }
            }

            // The transaction is already durable at this point. Returning Success prevents
            // the World from creating a second operation. The cache flag exposes the problem.
            response.Message = $"Listing committed, but cache refresh failed: {failure}";
            Console.WriteLine($"Bazaar cache refresh failed after commit {plan.OperationId}: {failure}");
            _bazaarManager.RememberCommitResponse(plan.OperationId, response);
            return response;
        }
    }
}
