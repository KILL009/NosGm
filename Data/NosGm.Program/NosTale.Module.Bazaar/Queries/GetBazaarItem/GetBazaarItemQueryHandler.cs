using FluentValidation;
using MediatR;
using NosGm.Data;
using NosGm.GameObject.Modules.Bazaar.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetBazaarItem
{
    internal class GetBazaarItemQueryHandler : IRequestHandler<GetBazaarItemQuery, BazaarItemDTO>
    {
        private readonly GetBazaarItemValidator _requestValidator = new();
        private readonly BazaarManager _bazaarManager;

        public GetBazaarItemQueryHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<BazaarItemDTO> Handle(GetBazaarItemQuery request, CancellationToken cancellation)
        {
            await _requestValidator.ValidateAndThrowAsync(request);

            Console.WriteLine($"Trying to get itemid: {request.Id}");
            if (!_bazaarManager.BazaarItems.ContainsKey(request.Id))
            {
                return null;
            }

            return _bazaarManager.BazaarItems[request.Id];
        }
    }
}
