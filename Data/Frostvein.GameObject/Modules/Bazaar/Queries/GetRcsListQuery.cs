using FluentValidation;
using MediatR;
using Frostvein.Packets.CustomPackets;

namespace Frostvein.GameObject.Modules.Bazaar.Queries
{
    public class GetRcsListQuery : IRequest<string>
    {
        public RcsPacketModel Model { get; set; }
    }

    public class GetRcsListQueryValidator : AbstractValidator<GetRcsListQuery>
    {
        public GetRcsListQueryValidator()
        {
            RuleFor(m => m.Model).NotNull();
        }
    }
}
