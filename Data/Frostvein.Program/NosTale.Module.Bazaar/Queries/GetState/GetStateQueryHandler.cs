using FluentValidation;
using MediatR;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetState
{
    internal class GetStateQueryHandler : IRequestHandler<GetStateQuery, bool>
    {
        private readonly GetStateQueryValidator _requestValidator = new();
        private readonly BazaarManager _bazaarManager;

        public GetStateQueryHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<bool> Handle(GetStateQuery request, CancellationToken cancellationToken)
        {
            await _requestValidator.ValidateAndThrowAsync(request);
            return _bazaarManager.BazaarItemStates.Any(s => s == request.Id);
        }
    }
}
