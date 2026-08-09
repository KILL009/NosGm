using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace NosGm.Handler.PacketHandler.Family
{
    public class UseFamilySkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseFamilySkillPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseFamilySkill(FwsPacket fwsPacket)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null && Session.Character.Family.FamilySkillMissions.FirstOrDefault(s => s.ItemVNum == fwsPacket.ItemVNum && s.CurrentValue == 1) is FamilySkillMission fsm && fsm != null)
            {
                if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                    return;
                }
                if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familydeputy || Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                {
                    switch (fwsPacket.ItemVNum)
                    {
                        case 9600:
                            try
                            {
                                ConfigurationServiceClient
                                    .RunWithConfigurationMutationBarrier(() =>
                                    {
                                        ConfigurationObject current =
                                            ServerManager.Instance.Configuration;
                                        var updated = new ConfigurationObject
                                        {
                                            MaxGold = current.MaxGold,
                                            TimeExpBuff = DateTime.Now.AddMinutes(60),
                                            TimeGoldBuff = current.TimeGoldBuff
                                        };

                                        // Publish the authoritative state first. If gRPC
                                        // fails, no local gameplay effect is applied and
                                        // the mission remains available for a later retry.
                                        ConfigurationServiceClient.Instance
                                            .UpdateConfigurationObject(updated);
                                        ServerManager.Instance.Configuration = updated;

                                        foreach (ClientSession s in
                                                 ServerManager.Instance.Sessions)
                                        {
                                            s.Character.AddStaticBuff(new StaticBuffDTO
                                            {
                                                CardId = 360,
                                                CharacterId = s.Character.CharacterId,
                                                RemainingTime = (int)(updated.TimeExpBuff - DateTime.Now).TotalSeconds
                                            });
                                        }
                                    });
                            }
                            catch (Exception exception)
                            {
                                Logger.Error(
                                    "[CONFIG_GRPC] Family EXP buff publication failed closed; no gameplay effect was applied.",
                                    exception);
                                Session.SendPacket(
                                    "info The family buff could not be activated because the configuration service is unavailable.");
                                return;
                            }
                            break;

                        case 9601:
                            try
                            {
                                ConfigurationServiceClient
                                    .RunWithConfigurationMutationBarrier(() =>
                                    {
                                        ConfigurationObject current =
                                            ServerManager.Instance.Configuration;
                                        var updated = new ConfigurationObject
                                        {
                                            MaxGold = current.MaxGold,
                                            TimeExpBuff = current.TimeExpBuff,
                                            TimeGoldBuff = DateTime.Now.AddMinutes(60)
                                        };

                                        // Keep publication and gameplay effects ordered:
                                        // gRPC authority must accept the new snapshot first.
                                        ConfigurationServiceClient.Instance
                                            .UpdateConfigurationObject(updated);
                                        ServerManager.Instance.Configuration = updated;

                                        foreach (ClientSession s in
                                                 ServerManager.Instance.Sessions)
                                        {
                                            s.Character.AddStaticBuff(new StaticBuffDTO
                                            {
                                                CardId = 361,
                                                CharacterId = s.Character.CharacterId,
                                                RemainingTime = (int)(updated.TimeGoldBuff - DateTime.Now).TotalSeconds
                                            });
                                        }
                                    });
                            }
                            catch (Exception exception)
                            {
                                Logger.Error(
                                    "[CONFIG_GRPC] Family gold buff publication failed closed; no gameplay effect was applied.",
                                    exception);
                                Session.SendPacket(
                                    "info The family buff could not be activated because the configuration service is unavailable.");
                                return;
                            }
                            break;

                        case 9602:
                            if (ServerManager.Instance.ChannelId != 51) return;
                            if (Session.Character.Family.FamilyFaction != (byte)FactionType.Angel) return;
                            if (ServerManager.Instance.Act4AngelStat.Mode != 0) return;
                            if (ServerManager.Instance.Act4DemonStat.Mode != 0) return;
                            ServerManager.Instance.Act4AngelStat.Percentage += 2000;
                            break;
                        case 9603:
                            if (ServerManager.Instance.ChannelId != 51) return;
                            if (Session.Character.Family.FamilyFaction != (byte)FactionType.Demon) return;
                            if (ServerManager.Instance.Act4AngelStat.Mode != 0) return;
                            if (ServerManager.Instance.Act4DemonStat.Mode != 0) return;
                            ServerManager.Instance.Act4DemonStat.Percentage += 2000;
                            break;

                        default:
                            return;
                    }
                    Session.Character.Family.InsertFamilyLog(FamilyLogType.SkillUse, characterName: Session.Character.Name, itemVNum: (fwsPacket.ItemVNum - 600));
                    fsm.CurrentValue = 0;
                    Session.Character.Family.SaveMission(fsm);
                    Session.Character.LastFamilyAction = DateTime.Now;
                }
            }
        }

        #endregion
    }
}
