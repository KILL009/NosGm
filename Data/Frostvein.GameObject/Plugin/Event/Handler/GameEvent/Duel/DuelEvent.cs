using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Frostvein.GameObject.Event
{
    public static class DuelEvent
    {
        private static readonly TimeSpan DuelTime = new(0, 5, 0);

        public static void SendInMapAfter(this MapInstance map, double sec, string packet)
        {
            Observable.Timer(TimeSpan.FromSeconds(sec)).Subscribe(o => { map.Broadcast(packet); });
        }
        private static void RemovePet(ClientSession session)
        {
            foreach (var mateTeam in session.Character.Mates?.Where(sess => sess.IsTeamMember))
            {
                if (mateTeam == null) continue;
                mateTeam.RemoveTeamMember(true);
            }
        }

        private const short MapVnum = 2101;

        public static void GenerateOneVersusOne()
        {
            ClientSession member;
            ClientSession opponent;
            var you = ServerManager.Instance.Sessions.Where(s => s.Character?.IsIn1v1Queue == true && s.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance);
            var map = ServerManager.GenerateMapInstance(MapVnum, MapInstanceType.NormalInstance, new InstanceBag());
            int i = 0;

            foreach (var s in you)
            {
                i++;
                if (i == 1)
                {
                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map.MapInstanceId, 19, 26, false);
                    s.Character.IsIn1v1Queue = false;
                    s.Character.IsCurrentlyIn1v1 = true;
                    s.Character.DuelCount += 1;
                    member = s;


                }
                if (i == 2)
                {
                    ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, map.MapInstanceId, 20, 12, false);
                    s.Character.IsIn1v1Queue = false;
                    s.Character.IsCurrentlyIn1v1 = true;
                    s.Character.DuelCount += 1;
                    opponent = s;
                }

            }

            ServerManager.Instance.StartedEvents.Remove(EventType.DUELEVENT);
            OneVersusOneTask.Run(map);
        }

        public class OneVersusOneTask
        {
            public static void Run(MapInstance mapinstance)
            {
                mapinstance.Sessions.Where(s => s.Character != null).ToList().ForEach(s =>
                {

                    mapinstance.Broadcast("msg 0 The Duel will begin in 30 seconds.");
                    mapinstance.SendInMapAfter(10, "msg 0 The Duel will begin in 20 seconds.");
                    mapinstance.SendInMapAfter(20, "msg 0 The Duel will begin in 10 seconds.");
                    mapinstance.SendInMapAfter(25, "ta_s 1");

                    foreach (ClientSession sess in mapinstance.Sessions)
                    {
                        sess.Character.DisableBuffs(BuffType.All);
                        sess.Character.NoMove = true;
                        sess.Character.NoAttack = true;
                        sess.SendPacket(sess.Character.GenerateCond());
                        RemovePet(sess);
                    }
                    Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                    {
                        foreach (ClientSession sess in mapinstance.Sessions)
                        {
                            sess.Character.NoMove = false;
                            sess.Character.NoAttack = false;
                            sess.SendPacket(sess.Character.GenerateCond());

                        }
                        mapinstance.Broadcast("msg 0 The Duel started. Good luck!");
                        mapinstance.IsPVP = true;
                    });
                });
                var cancellationToken = new CancellationTokenSource();
                Observable.Interval(TimeSpan.FromSeconds(300)).Timeout(DuelTime).Subscribe(_ =>
                {
                    Logger.Log.Info("[DuelSystem] Disposed MapInstance successfully");
                    cancellationToken.Cancel();
                }, () => { EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(30), new EventContainer(mapinstance, EventActionType.DISPOSEMAP, null)); }, cancellationToken.Token);
            }
        }

    }
}