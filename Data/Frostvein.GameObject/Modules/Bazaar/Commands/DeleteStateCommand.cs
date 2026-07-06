using FluentValidation;
using MediatR;

namespace Frostvein.GameObject.Modules.Bazaar.Commands
{
    public class DeleteStateCommand : IRequest<bool>
    {
        public long Id { get; set; }
    }

    public class DeleteStateCommandValidator : AbstractValidator<DeleteStateCommand>
    {
        public DeleteStateCommandValidator()
        {
            RuleFor(m => m.Id).NotEmpty();
        }
    }
}
