using FluentValidation;
using MediatR;

namespace NosGm.GameObject.Modules.Bazaar.Commands
{
    public class SetStateCommand : IRequest<bool>
    {
        public long Id { get; set; }
    }

    public class SetStateCommandValidator : AbstractValidator<SetStateCommand>
    {
        public SetStateCommandValidator()
        {
            RuleFor(m => m.Id).NotEmpty();
        }
    }
}
