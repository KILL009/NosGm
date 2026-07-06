using Frostvein.Domain;
using Frostvein.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Helpers;
using static System.Collections.Specialized.BitVector32;
using Frostvein.GameObject.Service;

namespace Frostvein.GameObeject.Instances
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