using MediatR;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetRcbList
{
    /// <summary>
    /// Uses the hardened global-search renderer directly instead of executing the legacy
    /// handler first. This prevents malformed historical listings from producing noisy
    /// NullReferenceException messages before the safe response is generated.
    /// </summary>
    internal sealed class GetRcbListAuthoritativeQueryHandler : IRequestHandler<GetRcbListQuery, string>
    {
        private readonly GetRcbListFallbackBehavior _renderer;

        public GetRcbListAuthoritativeQueryHandler(BazaarManager bazaarManager)
        {
            _renderer = new GetRcbListFallbackBehavior(bazaarManager);
        }

        public Task<string> Handle(GetRcbListQuery request, CancellationToken cancellationToken)
        {
            return _renderer.Handle(
                request,
                () => Task.FromResult(string.Empty),
                cancellationToken);
        }
    }
}
