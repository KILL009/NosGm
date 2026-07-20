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

            long bazaarItemId = plan?.Listing?.BazaarItemId ?? 0;
            string failure = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (_bazaarManager.TryRefreshListing(bazaarItemId, out failure))
                {
                    response.CacheRefreshed = true;
                    response.Message = result == BazaarListingResult.AlreadyCommitted
                        ? "Listing was already committed and the service cache was recovered."
                        : "Listing committed and cached by the NosBazaar service.";
                    return response;
                }

                if (attempt < 3)
                {
                    await Task.Delay(50 * attempt, cancellationToken);
                }
            }

            // The transaction is already durable at this point. Returning the successful
            // result prevents the World from retrying with a new OperationId and creating a
            // split-brain inventory. The explicit flag and message surface the cache problem.
            response.Message = $"Listing committed, but cache refresh failed: {failure}";
            Console.WriteLine($"Bazaar cache refresh failed after commit {plan.OperationId}: {failure}");
            return response;
        }
    }
}
