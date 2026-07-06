using FluentValidation;
using MediatR;
using Frostvein.Data;

namespace Frostvein.GameObject.Modules.Bazaar.Queries
{
    public class GetBazaarItemQuery : IRequest<BazaarItemDTO>
    {
        public long Id { get; set; }
    }

    public class GetBazaarItemValidator : AbstractValidator<GetBazaarItemQuery>
    {
        public GetBazaarItemValidator()
        {
            RuleFor(m => m.Id).NotEmpty();
        }
    }
}
