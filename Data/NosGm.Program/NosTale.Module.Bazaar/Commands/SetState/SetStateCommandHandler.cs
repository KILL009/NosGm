using FluentValidation;
using MediatR;
using NosGm.GameObject.Modules.Bazaar.Commands;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Commands.SetState
{
    internal class SetStateCommandHandler : IRequestHandler<SetStateCommand, bool>
    {
        private readonly SetStateCommandValidator _commandValidator = new();
        private readonly BazaarManager _bazaarManager;
        private int isBusy = 0;

        public SetStateCommandHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<bool> Handle(SetStateCommand command, CancellationToken cancellationToken)
        {
            await _commandValidator.ValidateAndThrowAsync(command);

            if (_bazaarManager.BazaarItemStates.Any(s => s.Equals(command.Id)))
            {
                return false;
            }
            
            if (Interlocked.CompareExchange(ref isBusy, 1, 0) == 1) return false;

            try
            {
                _bazaarManager.BazaarItemStates.Add(command.Id);

            }
            finally
            {
                isBusy = 0;
            }

            return true;
        }
    }
}
