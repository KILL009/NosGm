using FluentValidation;
using MediatR;
using NosGm.GameObject.Modules.Bazaar.Commands;
using System;
using NosGm.DAL;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Commands.DeleteItem
{
    internal class DeleteBazaarItemHandler : IRequestHandler<DeleteBazaarItemCommand, bool>
    {
        private readonly DeleteBazaarItemCommandValidator _commandValidator = new();
        private readonly BazaarManager _manager;

        public DeleteBazaarItemHandler(BazaarManager manager)
        {
            _manager = manager;
        }

        public async Task<bool> Handle(DeleteBazaarItemCommand command, CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command);

            lock (_manager.GetItemLock(command.Id))
            {
                var oldItemCount = _manager.BazaarItems.Count;
                var oldLinkCount = _manager.BazaarItemLinks.Count;

                _manager.BazaarItems.TryRemove(command.Id, out _);
                _manager.BazaarItemLinks.TryRemove(command.Id, out _);
                DAOFactory.BazaarItemDAO.Delete(command.Id);

                Console.WriteLine($"Removed bazaar cache entry {command.Id}. " +
                                  $"Items: {oldItemCount}->{_manager.BazaarItems.Count}, " +
                                  $"Links: {oldLinkCount}->{_manager.BazaarItemLinks.Count}");
                return true;
            }
        }
    }
}
