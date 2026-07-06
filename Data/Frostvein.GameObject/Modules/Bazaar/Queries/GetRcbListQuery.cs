using FluentValidation;
using MediatR;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.GameObject.Packets.ClientPackets;

namespace Frostvein.GameObject.Modules.Bazaar.Queries
{
    public class GetRcbListQuery : IRequest<string>
    {
        public CBListPacket Packet { get; set; }
    }

    public class GetRcbListQueryValidator : AbstractValidator<GetRcbListQuery>
    {
        public GetRcbListQueryValidator()
        {
            RuleFor(m => m.Packet).NotNull();
        }
    }
}
