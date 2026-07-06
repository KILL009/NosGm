using Frostvein.Packets.Packets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Frostvein.Handler.PacketHandler.ScriptedInstance
{
    public class TaCallPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TaCallPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Call(TaCallPacket packet)
        {
            try
            {
                var arenateam = ServerManager.Instance.ArenaTeams.ToList()
                    .FirstOrDefault(s => s.Any(o => o.Session == Session));
                if (arenateam == null ||
                    Session.CurrentMapInstance.MapInstanceType != MapInstanceType.TalentArenaMapInstance)
                {
                    return;
                }

                IEnumerable<ArenaTeamMember> ownteam = arenateam.Replace(s =>
                    s.ArenaTeamType == arenateam?.FirstOrDefault(e => e.Session == Session)?.ArenaTeamType);
                var client = ownteam.Where(s => s.Session != Session).OrderBy(s => s.Order).Skip(packet.CalledIndex)
                    .FirstOrDefault()?.Session;
                var memb = arenateam.FirstOrDefault(s => s.Session == client);
                if (client == null || client.CurrentMapInstance != Session.CurrentMapInstance || memb == null ||
                    memb.LastSummoned != null || ownteam.Sum(s => s.SummonCount) >= 5)
                {
                    return;
                }

                memb.SummonCount++;
                arenateam.ToList().ForEach(arenauser =>
                {
                    arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaP(2, true));
                });
                var arenaTeamMember = arenateam.FirstOrDefault(s => s.Session == client);
                if (arenaTeamMember != null)
                {
                    arenaTeamMember.LastSummoned = DateTime.Now;
                }

                Session.CurrentMapInstance.Broadcast(Session.Character.GenerateEff(4432));

                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(o =>
                {
                    client.SendPacket(
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 3), 0));
                    client.SendPacket(
                        client.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 3), 10));
                });

                Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                {
                    client.SendPacket(
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 2), 0));
                    client.SendPacket(
                        client.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 2), 10));
                });

                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(o =>
                {
                    client.SendPacket(
                        UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 1), 0));
                    client.SendPacket(
                        client.Character.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_CALLED"), 1), 10));
                });

                var x = Session.Character.PositionX;
                var y = Session.Character.PositionY;
                const byte TIMER = 30;
                Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(o =>
                {
                    Session.CurrentMapInstance.Broadcast($"ta_t 0 {client.Character.CharacterId} {TIMER}");
                    client.Character.PositionX = x;
                    client.Character.PositionY = y;
                    Session.CurrentMapInstance.Broadcast(client.Character.GenerateTp());

                    client.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Nothing));
                });

                Observable.Timer(TimeSpan.FromSeconds(TIMER + 3)).Subscribe(o =>
                {
                    var lastsummoned = arenateam.FirstOrDefault(s => s.Session == client)?.LastSummoned;
                    if (lastsummoned == null || ((DateTime)lastsummoned).AddSeconds(TIMER) >= DateTime.Now)
                    {
                        return;
                    }

                    var firstOrDefault = arenateam.FirstOrDefault(s => s.Session == client);
                    if (firstOrDefault != null)
                    {
                        firstOrDefault.LastSummoned = null;
                    }

                    var bufftodisable = new List<BuffType> { BuffType.Bad };
                    client.Character.DisableBuffs(bufftodisable);
                    client.Character.Hp = (int)client.Character.HPLoad();
                    client.Character.Mp = (int)client.Character.MPLoad();
                    client.SendPacket(client.Character.GenerateStat());

                    client.Character.PositionX = memb.ArenaTeamType == ArenaTeamType.ERENIA ? (short)120 : (short)19;
                    client.Character.PositionY = memb.ArenaTeamType == ArenaTeamType.ERENIA ? (short)39 : (short)40;
                    Session?.CurrentMapInstance?.Broadcast(client?.Character?.GenerateTp());
                    client.SendPacket(UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));
                });
            }
            catch
            {
            }
        }

        #endregion
    }
}