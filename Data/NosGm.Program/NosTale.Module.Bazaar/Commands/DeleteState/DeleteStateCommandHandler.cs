using FluentValidation;
using MediatR;
using NosGm.GameObject.Modules.Bazaar.Commands;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Core.Extensions.Concurrent;

namespace NosTale.Module.Bazaar.Commands.DeleteState
{
    internal class DeleteStateCommandHandler : IRequestHandler<DeleteStateCommand, bool>
    {
        private readonly DeleteStateCommandValidator _commandValidator = new();
        private readonly BazaarManager _bazaarManager;

        public DeleteStateCommandHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<bool> Handle(DeleteStateCommand command, CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command);

            if (!_bazaarManager.BazaarItemStates.Any(s => s.Equals(command.Id)))
            {
                return false;
            }

            _bazaarManager.BazaarItemStates.RemoveWhere(s => s != command.Id, out var result);
            _bazaarManager.BazaarItemStates = result;
            return true;
        }
    }
}