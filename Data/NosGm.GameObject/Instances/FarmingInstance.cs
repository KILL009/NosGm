using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Helpers;
using static System.Collections.Specialized.BitVector32;
using NosGm.GameObject.Service;

namespace NosGm.GameObeject.Instances
{
    public static class FarmingInstance
    {
        private static readonly TimeSpan FarmingTime = new(0, 30, 0);

        public static void GenerateFarmingInstance(ClientSession s)
        {
            if (s == null) { return; }

            s.Character.IsOnMapInstance = true;
            MapInstance mapID = null;
            mapID = ServerManager.GenerateMapInstance(1, MapInstanceType.BaseMapInstance, new InstanceBag());
            ServerManager.Instance.ChangeMapInstance(s.Character.CharacterId, mapID.MapInstanceId, 80, 116);
            var cancellationToken = new CancellationTokenSource();

            s.Character.MapInstance.Clock.TotalSecondsAmount = 18000;
            s.Character.MapInstance.Clock.SecondsRemaining = 18000;
            s.Character.MapInstance.Clock.StartClock();
            s.SendPacket(s.Character.MapInstance.Clock.GetClock());

            Observable.Interval(TimeSpan.FromMinutes(30)).Timeout(FarmingTime).Subscribe(_ =>
            {
                s.Character.IsOnMapInstance = false;
                cancellationToken.Cancel();

            }, () => { EventHelper.Instance.ScheduleEvent(TimeSpan.FromMinutes(30), new EventContainer(mapID, EventActionType.DISPOSEMAP, null)); }, cancellationToken.Token);
        }
    }
}