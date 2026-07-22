using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace NosGm.GameObject.Event
{
    public static class TalentArena
    {
        #region Methods

        public static void Run()
        {
            RegisteredParticipants = new ThreadSafeSortedList<long, ClientSession>();
            RegisteredGroups = new ThreadSafeSortedList<long, Group>();
            PlayingGroups = new ThreadSafeSortedList<long, List<Group>>();

            ServerManager.Shout(Language.Instance.GetMessageFromKey("TALENTARENA_OPEN"), true);

            var groupingThread = new GroupingThread();
            Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer => groupingThread.Run());

            var matchmakingThread = new MatchmakingThread();
            Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(observer => matchmakingThread.Run());

            IsRunning = true;

            Observable.Timer(TimeSpan.FromMinutes(30)).Subscribe(observer =>
            {
                groupingThread.RequestStop();
                matchmakingThread.RequestStop();
                RegisteredParticipants.ClearAll();
                RegisteredGroups.ClearAll();
                IsRunning = false;
                ServerManager.Instance.StartedEvents.Remove(EventType.TALENTARENA);
            });
        }

        #endregion

        #region Properties

        public static bool IsRunning { get; set; }

        public static ThreadSafeSortedList<long, List<Group>> PlayingGroups { get; set; }

        public static ThreadSafeSortedList<long, Group> RegisteredGroups { get; set; }

        public static ThreadSafeSortedList<long, ClientSession> RegisteredParticipants { get; set; }

        #endregion

        #region Classes

        private class BattleThread
        {
            #region Properties

            private List<ClientSession> Characters { get; set; }

            #endregion

            #region Methods

            public void Run(List<Group> groups)
            {
                Characters = groups[0].Sessions.GetAllItems().Concat(groups[1].Sessions.GetAllItems()).ToList();
            }

            #endregion
        }

        private class GroupingThread
        {
            #region Members

            private bool _shouldStop;

            #endregion

            #region Methods

            public void RequestStop()
            {
                _shouldStop = true;
            }

            public void Run()
            {
                byte[] levelCaps = { 40, 50, 60, 70, 80, 85, 90, 95, 100, 120, 150, 180, 255 };
                while (!_shouldStop)
                {
                    var groups = from sess in RegisteredParticipants.GetAllItems()
                                 group sess by Array.Find(levelCaps, s => s > sess.Character.Level)
                        into grouping
                                 select grouping;
                    foreach (var group in groups)
                        foreach (var grp in group.ToList().Split(3).Where(s => s.Count == 3))
                        {
                            var g = new Group
                            {
                                GroupType = GroupType.TalentArena,
                                TalentArenaBattle = new TalentArenaBattle
                                {
                                    GroupLevel = group.Key
                                }
                            };

                            foreach (var sess in grp)
                            {
                                RegisteredParticipants.Remove(sess);
                                g.JoinGroup(sess);
                                sess.SendPacket(UserInterfaceHelper.GenerateBSInfo(1, 3, -1, 6));
                                Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(observer =>
                                    sess?.SendPacket(UserInterfaceHelper.GenerateBSInfo(1, 3, 300, 1)));
                            }

                            RegisteredGroups[g.GroupId] = g;
                        }

                    Thread.Sleep(5000);
                }
            }

            #endregion
        }

        private class MatchmakingThread
        {
            #region Members

            private bool _shouldStop;

            #endregion

            #region Methods

            public void RequestStop()
            {
                _shouldStop = true;
            }

            public void Run()
            {
                while (!_shouldStop)
                {
                    var groups = from grp in RegisteredGroups.GetAllItems()
                                 where grp.TalentArenaBattle != null
                                 group grp by grp.TalentArenaBattle.GroupLevel
                        into grouping
                                 select grouping;

                    foreach (var group in groups)
                    {
                        Group prevGroup = null;

                        foreach (var g in group)
                        {
                            if (prevGroup == null)
                            {
                                prevGroup = g;
                            }
                            else
                            {
                                RegisteredGroups.Remove(g);
                                RegisteredGroups.Remove(prevGroup);

                                var mapInstance = ServerManager.GenerateMapInstance(2015,
                                        MapInstanceType.NormalInstance, new InstanceBag());
                                mapInstance.IsPVP = true;

                                g.TalentArenaBattle.MapInstance = mapInstance;
                                prevGroup.TalentArenaBattle.MapInstance = mapInstance;

                                g.TalentArenaBattle.Side = 0;
                                prevGroup.TalentArenaBattle.Side = 1;

                                g.TalentArenaBattle.Calls = 5;
                                prevGroup.TalentArenaBattle.Calls = 5;

                                var gs = g.Sessions.GetAllItems().Concat(prevGroup.Sessions.GetAllItems());
                                foreach (var sess in gs)
                                {
                                    sess.SendPacket(UserInterfaceHelper.GenerateBSInfo(1, 3, -1, 2));
                                }

                                Thread.Sleep(1000);
                                foreach (var sess in gs)
                                {
                                    sess.SendPacket(UserInterfaceHelper.GenerateBSInfo(2, 3, 0, 0));
                                    sess.SendPacket(UserInterfaceHelper.GenerateTeamArenaClose());
                                }

                                Thread.Sleep(5000);
                                foreach (var sess in gs)
                                {
                                    sess.SendPacket(UserInterfaceHelper.GenerateTeamArenaMenu(0, 0, 0, 0, 0));
                                    short x = 125;
                                    if (sess.Character.Group.TalentArenaBattle.Side == 0)
                                    {
                                        x = 15;
                                    }

                                    ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId,
                                            mapInstance.MapInstanceId, x, 39);
                                    sess.SendPacketAfter(UserInterfaceHelper.GenerateTeamArenaMenu(3, 0, 0, 60, 0),
                                            5000);
                                }

#warning TODO: Other Setup stuff

                                PlayingGroups[g.GroupId] = new List<Group> { g, prevGroup };

                                var battleThread = new BattleThread();
                                Observable.Timer(TimeSpan.FromSeconds(0)).Subscribe(observer =>
                                        battleThread.Run(PlayingGroups[g.GroupId]));

                                prevGroup = null;
                            }
                        }
                    }

                    Thread.Sleep(5000);
                }
            }

            #endregion
        }

        #endregion
    }
}