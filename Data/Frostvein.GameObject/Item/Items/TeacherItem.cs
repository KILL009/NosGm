
using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;


namespace Frostvein.GameObject
{
    public class TeacherItem : Item
    {
        #region Instantiation

        public TeacherItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            if (session.Character.IsVehicled)
            {
                session.SendPacket(
                    session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance) return;

            if (packetsplit == null) return;

            void releasePet(MateType mateType, Guid itemToRemoveId)
            {
                if (int.TryParse(packetsplit[3], out var mateTransportId))
                {
                    var mate = session.Character.Mates.Find(s =>
                        s.MateTransportId == mateTransportId && s.MateType == mateType);
                    if (mate != null)
                    {
                        if (!mate.IsTeamMember)
                        {
                            var mateInventory = mate.GetInventory();
                            if (mateInventory.Count > 0)
                            {
                                session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"),
                                        0));
                            }
                            else
                            {
                                session.Character.Mates.Remove(mate);
                                byte i = 0;
                                session.Character.Mates.Where(s => s.MateType == MateType.Partner).ToList().ForEach(s =>
                                {
                                    s.GetInventory().ForEach(item => item.Type = (InventoryType)(13 + i));
                                    s.PetId = i;
                                    i++;
                                });
                                session.SendPacket(
                                    UserInterfaceHelper.GenerateInfo(
                                        Language.Instance.GetMessageFromKey("PET_RELEASED")));
                                session.SendPacket(UserInterfaceHelper.GeneratePClear());
                                session.SendPackets(session.Character.GenerateScP());
                                session.SendPackets(session.Character.GenerateScN());
                                session.CurrentMapInstance?.Broadcast(mate.GenerateOut());
                                session.Character.Inventory.RemoveItemFromInventory(itemToRemoveId);
                            }
                        }
                        else
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("PET_IN_TEAM_UNRELEASABLE"), 0));
                        }
                    }
                }
            }

            if (BCards.Count > 0 && session.Character.MapInstance == session.Character.Miniland)
            {
                BCards.ForEach(c => c.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                return;
            }

            switch (Effect)
            {
                case 10:
                    if (int.TryParse(packetsplit[3], out var mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Pet || mate.Loyalty >= 1000) return;
                        mate.Loyalty += 100;
                        if (mate.Loyalty > 1000) mate.Loyalty = 1000;
                        mate.GenerateXp(EffectValue);
                        session.SendPacket(mate.GenerateCond());
                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 5002));
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                case 11:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Pet ||
                            mate.Level >= session.Character.Level - 5 ||
                            mate.Level + 1 > GameConfiguration.MaxLevel) return;
                        mate.Level++;
                        session.CurrentMapInstance?.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 8), mate.PositionX,
                            mate.PositionY);
                        session.CurrentMapInstance?.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 198), mate.PositionX,
                            mate.PositionY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(mate.GenerateScPacket());
                    }

                    break;

                case 12:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Partner ||
                            mate.Level >= session.Character.Level - 5 ||
                            mate.Level + 1 > GameConfiguration.MaxLevel) return;
                        mate.Level++;
                        session.CurrentMapInstance?.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 8), mate.PositionX,
                            mate.PositionY);
                        session.CurrentMapInstance?.Broadcast(
                            StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 198), mate.PositionX,
                            mate.PositionY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        session.SendPacket(mate.GenerateScPacket());
                    }

                    break;

                case 13:
                    if (int.TryParse(packetsplit[3], out mateTransportId) &&
                        session.Character.Mates.FirstOrDefault(s => s.MateTransportId == mateTransportId) is Mate pet)
                    {
                        if (pet.MateType == MateType.Pet)
                            session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 1, mateTransportId, 2));
                        else
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_CHANGE_PARTNER_NAME"), 0));
                    }

                    break;

                case 14:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        if (session.Character.MapInstance == session.Character.Miniland)
                        {
                            var mate = session.Character.Mates.Find(s =>
                                s.MateTransportId == mateTransportId && s.MateType == MateType.Pet);
                            if (mate?.CanPickUp == false)
                            {
                                session.CurrentMapInstance.Broadcast(
                                    StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 5));
                                session.CurrentMapInstance.Broadcast(
                                    StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 5002));
                                mate.CanPickUp = true;
                                session.SendPackets(session.Character.GenerateScP());
                                session.SendPacket(
                                    session.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("PET_CAN_PICK_UP"), 10));
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            }
                        }
                        else
                        {
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_IN_MINILAND"),
                                    12));
                        }
                    }

                    break;

                case 16:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Pet || mate.Level == 1) return;
                        mate.Level--;
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                case 17:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {

                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate?.IsSummonable == false)
                        {
                            mate.IsSummonable = true;
                            session.SendPackets(session.Character.GenerateScP());
                            session.SendPacket(session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("PET_SUMMONABLE"), mate.Name), 10));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("PET_SUMMONABLE"), mate.Name), 0));
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }

                        if (session.Character.MapInstance?.MapInstanceType == MapInstanceType.ArenaInstance)
                        {
                            return;
                        }
                    }

                    break;

                case 18:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Partner || mate.Level == 1) return;
                        mate.Level--;
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                // custom food
                case 20:
                    if (int.TryParse(packetsplit[3], out mateTransportId))
                    {
                        var mate = session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate == null || mate.MateType != MateType.Pet || session.Character.Level < 99 || mate.Level + 1 > GameConfiguration.MaxLevel)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_PERMITTED"), 10));
                            return;
                        }
                        mate.Level = 98;
                        session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("FOOD_USED_ON"), mate.Name), 10));
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 8), mate.PositionX, mate.PositionY);
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId, 198), mate.PositionX, mate.PositionY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                case 1000:
                    releasePet(MateType.Pet, inv.Id);
                    break;

                case 1001:
                    releasePet(MateType.Partner, inv.Id);
                    break;

                default:
                    //LOGGER($"[HANDLER] Handler not found for: {GetType()} | {VNum} | {Effect} | {EffectValue}");
                    break;
            }
        }

        #endregion
    }
}