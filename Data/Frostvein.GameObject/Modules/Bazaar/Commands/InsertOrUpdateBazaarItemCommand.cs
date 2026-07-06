using FluentValidation;
using MediatR;
using Frostvein.Data;

namespace Frostvein.GameObject.Modules.Bazaar.Commands
{
    public class InsertOrUpdateBazaarItemCommand : IRequest<long>
    {
        public BazaarItemDTO BazaarItem { get; set; }
    }

    public class InsertOrUpdateBazaarItemCommandValidator : AbstractValidator<InsertOrUpdateBazaarItemCommand>
    {
        public InsertOrUpdateBazaarItemCommandValidator()
        {
            RuleFor(m => m.BazaarItem).NotNull();
        }
    }
}
