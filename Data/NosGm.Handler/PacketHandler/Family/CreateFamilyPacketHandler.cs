using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using System;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace NosGm.Handler.PacketHandler.Family
{
    public class CreateFamilyPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CreateFamilyPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void CreateFamily(CreateFamilyPacket createFamilyPacket)
        {
            if (Session.Character.Group?.GroupType == GroupType.Group && Session.Character.Group.SessionCount == 3)
            {
                foreach (ClientSession session in Session.Character.Group.Sessions.GetAllItems())
                {
                    if (session.Character.Family != null || session.Character.FamilyCharacter != null)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(
                                Language.Instance.GetMessageFromKey("PARTY_MEMBER_IN_FAMILY")));
                        return;
                    }
                    else if (session.Character.LastFamilyLeave > DateTime.Now.AddDays(-1).Ticks)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(
                                Language.Instance.GetMessageFromKey("PARTY_MEMBER_HAS_PENALTY")));
                        return;
                    }
                }

                if (Session.Character.Gold < 200000)
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY")));
                    return;
                }

                string name = createFamilyPacket.CharacterName;
                if (DAOFactory.FamilyDAO.LoadByName(name) != null)
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(
                            Language.Instance.GetMessageFromKey("FAMILY_NAME_ALREADY_USED")));
                    return;
                }

                Session.Character.Gold -= 200000;
                Session.SendPacket(Session.Character.GenerateGold());
                FamilyDTO family = new FamilyDTO
                {
                    Name = name,
                    FamilyExperience = 0,
                    FamilyLevel = 1,
                    FamilyMessage = "",
                    FamilyFaction = Session.Character.Faction != FactionType.None ? (byte)Session.Character.Faction : (byte)ServerManager.RandomNumber(1, 2),
                    MaxSize = 50,
                };
                DAOFactory.FamilyDAO.InsertOrUpdate(ref family);

                Logger.LogUserEvent("GUILDCREATE", Session.GenerateIdentity(), $"[FamilyCreate][{family.FamilyId}]");

                ServerManager.Instance.Broadcast(
                    UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("FAMILY_FOUNDED"), name), 0));
                foreach (ClientSession session in Session.Character.Group.Sessions.GetAllItems())
                {
                    session.Character.ChangeFaction(FactionType.None);
                    FamilyCharacterDTO familyCharacter = new FamilyCharacterDTO
                    {
                        CharacterId = session.Character.CharacterId,
                        DailyMessage = "",
                        Experience = 0,
                        Authority = Session.Character.CharacterId == session.Character.CharacterId
                            ? FamilyAuthority.Head
                            : FamilyAuthority.Familydeputy,
                        FamilyId = family.FamilyId,
                        Rank = 0
                    };
                    DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref familyCharacter);
                }

                ServerManager.Instance.FamilyRefresh(family.FamilyId);
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = family.FamilyId,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = "fhis_stc",
                    Type = MessageType.Family
                });
                Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                ServerManager.Instance.FamilyRefresh(family.FamilyId));
                Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(o =>
                ServerManager.Instance.FamilyRefresh(family.FamilyId));
            }
            //Works well be need to be moved from here + add the limited item (9142)
            else if (createFamilyPacket.CharacterName != null && Session.Character.Group == null && Session.Character.Inventory.Any(x => x.Item.VNum == 5787))
            {
                if (createFamilyPacket.CharacterName.Length < 4 || createFamilyPacket.CharacterName.Length > 14)
                {
                    Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("INVALID_CHARNAME")}");
                }
                else
                {
                    Regex rg = new Regex(@"^[A-Za-z0-9_äÄöÖüÜß~*<>°+-.!_-Ð™¤£±†‡×ßø^\S]+$");
                    if (rg.Matches(createFamilyPacket.CharacterName).Count == 1)
                    {
                        if (DAOFactory.CharacterDAO.LoadByName(createFamilyPacket.CharacterName) == null)
                        {
                            Session.Character.Name = createFamilyPacket.CharacterName;

                            if (Session.Character.Miniland == Session.Character.MapInstance)
                            {
                                ServerManager.Instance.JoinMiniland(Session, Session);
                            }
                            else
                            {
                                ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                    Session.Character.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY,
                                    true);
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                            }

                            Session.Character.Event.EmitEvent(new CharacterSaveEvent());
                            Session.Character.Inventory.RemoveItemFromInventory(Session.Character.Inventory.LoadByVNum<ItemInstance>(5787).Id);
                        }
                        else
                        {
                            Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("ALREADY_TAKEN")}");
                        }
                    }
                    else
                    {
                        Session.SendPacketFormat($"info {Language.Instance.GetMessageFromKey("INVALID_CHARNAME")}");
                    }

                }
            }
        }

        #endregion
    }
}