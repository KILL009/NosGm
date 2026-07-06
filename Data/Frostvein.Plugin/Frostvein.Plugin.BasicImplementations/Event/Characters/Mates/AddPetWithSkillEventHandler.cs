using ChickenAPI.Events;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events.Mates;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.Characters.Mates
{

    public class AddPetWithSkillEventHandler : GenericEventHandlerBase<AddPetWithSkillEvent>
    {
        protected override void Handle(AddPetWithSkillEvent e, CancellationToken cancellation)
        {
            Character character = e.Sender.Character;

            Mate mate = e.Sender.Mate;

            HandlePetWithSkill(character, mate);
        }

        private void HandlePetWithSkill(Character character, Mate mate)
        {
            if (mate == null)
            {
                return;
            }
            bool isUsingMate = true;
            if (!character.Mates.ToList().Any(s => s.IsTeamMember && s.MateType == mate.MateType))
            {
                isUsingMate = false;
                mate.IsTeamMember = true;
            }
            else
            {
                mate?.BackToMiniland();
            }

            character.Session.SendPacket($"ctl 2 {mate.MateTransportId} 3");
            character.Mates.Add(mate);
            character.Session.SendPacket(UserInterfaceHelper.GeneratePClear());
            character.Session.SendPackets(character.GenerateScP());
            character.Session.SendPackets(character.GenerateScN());
            if (!isUsingMate)
            {
                Parallel.ForEach(character.Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                {
                    if (ServerManager.Instance.ChannelId != 51 || character.Session.Character.Faction == s.Character.Faction)
                    {
                        s.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                    }
                    else
                    {
                        s.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                    }
                });

                character.Session.SendPacket(character.GeneratePinit());
                character.Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                character.Session.SendPackets(character.GenerateScP());
                character.Session.SendPackets(character.GenerateScN());
                character.Session.SendPackets(character.GeneratePst());
            }
        }
    }
}
