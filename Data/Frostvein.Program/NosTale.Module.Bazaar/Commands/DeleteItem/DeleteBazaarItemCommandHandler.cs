using FluentValidation;
using MediatR;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using System;
using Frostvein.DAL;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var exists = _manager.BazaarItems.ContainsKey(command.Id);

            if (!exists)
            {
                return false;
            }

            var oldItemCount = _manager.BazaarItems.Count;
            var oldLinkCount = _manager.BazaarItemLinks.Count;

            _manager.BazaarItems.TryRemove(command.Id, out _);
            _manager.BazaarItemLinks.TryRemove(command.Id, out _);

            DAOFactory.BazaarItemDAO.Delete(command.Id);
            Console.WriteLine($"Removed items successfully. Old count: {oldItemCount} | new count: {_manager.BazaarItems.Count}");
            Console.WriteLine($"Removed links successfully. Old count: {oldLinkCount} | new count: {_manager.BazaarItemLinks.Count}");

            return true;
        }
    }
}
