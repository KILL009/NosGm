using FluentValidation;
using MediatR;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetRcbList
{
    /// <summary>
    /// Authoritative global bazaar search handler. All requests are rendered through the
    /// hardened search path so malformed historical listings cannot abort the response.
    /// </summary>
    internal sealed class GetRcbListQueryHandler : IRequestHandler<GetRcbListQuery, string>
    {
        private readonly GetRcbListQueryValidator _requestValidator = new();
        private readonly GetRcbListFallbackBehavior _renderer;

        public GetRcbListQueryHandler(BazaarManager bazaarManager)
        {
            _renderer = new GetRcbListFallbackBehavior(bazaarManager);
        }

        public async Task<string> Handle(GetRcbListQuery request, CancellationToken cancellationToken)
        {
            await _requestValidator.ValidateAndThrowAsync(request, cancellationToken);

            return await _renderer.Handle(
                request,
                () => Task.FromResult(string.Empty),
                cancellationToken);
        }
    }
}
