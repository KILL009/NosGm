using FluentValidation;
using MediatR;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Modules.Bazaar.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Commands.InsertOrUpdateBazaarItem
{
    internal class InsertOrUpdateBazaarItemCommandHandler : IRequestHandler<InsertOrUpdateBazaarItemCommand, long>
    {
        private readonly InsertOrUpdateBazaarItemCommandValidator _commandValidator = new();
        private readonly BazaarManager _bazaarManager;

        public InsertOrUpdateBazaarItemCommandHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<long> Handle(InsertOrUpdateBazaarItemCommand command, CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command, cancellationToken);

            BazaarItemDTO item = command.BazaarItem;
            if (!command.RefreshOnly)
            {
                DAOFactory.BazaarItemDAO.InsertOrUpdate(ref item);
            }

            long bazaarItemId = item.BazaarItemId;
            if (!_bazaarManager.TryRefreshListing(bazaarItemId, out string failure))
            {
                Console.WriteLine($"Unable to refresh bazaar listing {bazaarItemId}: {failure}");
                return -1;
            }

            Console.WriteLine($"Bazaar listing {bazaarItemId} refreshed successfully.");
            return bazaarItemId;
        }
    }
}
