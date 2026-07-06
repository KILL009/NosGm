using FluentValidation;
using MediatR;

namespace Frostvein.GameObject.Modules.Bazaar.Commands
{
    public class DeleteBazaarItemCommand : IRequest<bool>
    {
        public long Id { get; set; }
    }

    public class DeleteBazaarItemCommandValidator : AbstractValidator<DeleteBazaarItemCommand>
    {
        public DeleteBazaarItemCommandValidator()
        {
            RuleFor(m => m.Id).NotEmpty();
        }
    }
}
