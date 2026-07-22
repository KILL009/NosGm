using FluentValidation;
using MediatR;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.GameObject.Packets.ClientPackets;

namespace NosGm.GameObject.Modules.Bazaar.Queries
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
