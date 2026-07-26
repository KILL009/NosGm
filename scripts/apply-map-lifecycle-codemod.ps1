param(
    [string]$MapInstancePath = "Data/NosGm.GameObject/Map/MapInstance.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $MapInstancePath)) {
    throw "MapInstance source not found: $MapInstancePath"
}

$content = Get-Content -LiteralPath $MapInstancePath -Raw
$original = $content
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

function Normalize-NewLines {
    param([string]$Value)
    return [regex]::Replace($Value, "`r`n|`n|`r", $newLine)
}

function Replace-LiteralOnce {
    param(
        [string]$Text,
        [string]$OldValue,
        [string]$NewValue,
        [string]$Description,
        [string]$AppliedMarker
    )

    $oldNormalized = Normalize-NewLines $OldValue
    $newNormalized = Normalize-NewLines $NewValue
    $first = $Text.IndexOf($oldNormalized, [StringComparison]::Ordinal)

    if ($first -lt 0) {
        if ($Text.Contains($AppliedMarker)) {
            return $Text
        }

        throw "Unable to find expected source for: $Description"
    }

    $second = $Text.IndexOf($oldNormalized, $first + $oldNormalized.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match for: $Description"
    }

    return $Text.Substring(0, $first) + $newNormalized + $Text.Substring($first + $oldNormalized.Length)
}

$content = Replace-LiteralOnce $content @'
using System.Reactive.Linq;
'@ @'
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
'@ "map lifecycle imports" "using System.Threading;"

$content = Replace-LiteralOnce $content @'
        private readonly Random _random;

        private IDisposable _mapLifeDisposable;
'@ @'
        private readonly Random _random;

        private static readonly long MapDiagnosticIntervalTicks = TimeSpan.FromSeconds(30).Ticks;

        private readonly ConcurrentDictionary<string, long> _lastDiagnosticLogTicks =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);

        private IDisposable _mapLifeDisposable;
'@ "bounded map diagnostics members" "MapDiagnosticIntervalTicks = TimeSpan.FromSeconds(30).Ticks"

$content = Replace-LiteralOnce $content @'
        public void StopLife()
        {
            _mapLifeDisposable?.Dispose();
        }
'@ @'
        public void StopLife()
        {
            IDisposable mapLifeDisposable = Interlocked.Exchange(ref _mapLifeDisposable, null);
            mapLifeDisposable?.Dispose();
        }
'@ "deterministic map-life shutdown" "Interlocked.Exchange(ref _mapLifeDisposable, null)"

$content = Replace-LiteralOnce $content @'
        public void AddDelayedMonster(MapMonster monster)
'@ @'
        private void LogMapException(Exception exception, [CallerMemberName] string operation = null)
        {
            if (exception == null || string.IsNullOrWhiteSpace(operation))
            {
                return;
            }

            long now = DateTime.UtcNow.Ticks;
            while (true)
            {
                long previous = _lastDiagnosticLogTicks.GetOrAdd(operation, 0);
                if (previous != 0 && now - previous < MapDiagnosticIntervalTicks)
                {
                    return;
                }

                if (_lastDiagnosticLogTicks.TryUpdate(operation, now, previous))
                {
                    break;
                }
            }

            Logger.Error(
                $"[MAP_OPERATION_FAILED] Operation={operation} MapId={Map?.MapId} Instance={MapInstanceId}",
                exception);
        }

        public void AddDelayedMonster(MapMonster monster)
'@ "throttled map diagnostics helper" "private void LogMapException(Exception exception"

$content = Replace-LiteralOnce $content @'
        public void DropItemByMonster(long? owner, DropDTO drop, short mapX, short mapY, bool isQuest = false)
        {
            try
            {
                var localMapX = mapX;
                var localMapY = mapY;
                var possibilities = new List<MapCell>();

                for (short x = -1; x < 2; x++)
                    for (short y = -1; y < 2; y++)
                    {
                        possibilities.Add(new MapCell { X = x, Y = y });
                    }

                foreach (var possibility in possibilities.OrderBy(s => ServerManager.RandomNumber()))
                {
                    localMapX = (short)(mapX + possibility.X);
                    localMapY = (short)(mapY + possibility.Y);
                    if (!Map.IsBlockedZone(localMapX, localMapY))
                    {
                        break;
                    }
                }

                var droppedItem = new MonsterMapItem(localMapX, localMapY, drop.ItemVNum, drop.Amount, owner ?? -1);
                DroppedList[droppedItem.TransportId] = droppedItem;
                Broadcast(
                    $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} {(isQuest ? 1 : 0)} {owner}");
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }
'@ @'
        public void DropItemByMonster(long? owner, DropDTO drop, short mapX, short mapY, bool isQuest = false)
        {
            try
            {
                var localMapX = mapX;
                var localMapY = mapY;
                var possibilities = new List<MapCell>();

                for (short x = -1; x < 2; x++)
                    for (short y = -1; y < 2; y++)
                    {
                        possibilities.Add(new MapCell { X = x, Y = y });
                    }

                foreach (var possibility in possibilities.OrderBy(s => ServerManager.RandomNumber()))
                {
                    localMapX = (short)(mapX + possibility.X);
                    localMapY = (short)(mapY + possibility.Y);
                    if (!Map.IsBlockedZone(localMapX, localMapY))
                    {
                        break;
                    }
                }

                var droppedItem = new MonsterMapItem(localMapX, localMapY, drop.ItemVNum, drop.Amount, owner ?? -1);
                DroppedList[droppedItem.TransportId] = droppedItem;
                Broadcast(
                    $"drop {droppedItem.ItemVNum} {droppedItem.TransportId} {droppedItem.PositionX} {droppedItem.PositionY} {(droppedItem.GoldAmount > 1 ? droppedItem.GoldAmount : droppedItem.Amount)} {(isQuest ? 1 : 0)} {owner}");
            }
            catch (Exception e)
            {
                LogMapException(e);
            }
        }
'@ "monster drop diagnostics" "LogMapException(e);"

$content = Replace-LiteralOnce $content @'
        public void LoadMonsters(IEnumerable<MapMonsterDTO> monsters = null)
        {
            if (monsters == null)
            {
                monsters = DAOFactory.MapMonsterDAO.LoadFromMap(Map.MapId).ToList();
            }

            foreach (var monster in monsters)
            {
                var tmp = new MapMonster(monster);
                if (!(tmp is MapMonster mapMonster))
                {
                    return;
                }

                mapMonster.Initialize(this);
                mapMonster.Initialize(this);
                var mapMonsterId = mapMonster.MapMonsterId;
                _monsters[mapMonsterId] = mapMonster;
                _mapMonsterIds[mapMonsterId] = mapMonsterId;
            }
        }
'@ @'
        public void LoadMonsters(IEnumerable<MapMonsterDTO> monsters = null)
        {
            if (monsters == null)
            {
                monsters = DAOFactory.MapMonsterDAO.LoadFromMap(Map.MapId).ToList();
            }

            foreach (var monster in monsters)
            {
                var tmp = new MapMonster(monster);
                if (!(tmp is MapMonster mapMonster))
                {
                    return;
                }

                mapMonster.Initialize(this);
                var mapMonsterId = mapMonster.MapMonsterId;
                _monsters[mapMonsterId] = mapMonster;
                _mapMonsterIds[mapMonsterId] = mapMonsterId;
            }
        }
'@ "single persisted monster initialization" "mapMonster.Initialize(this);$newLine                var mapMonsterId"

$content = Replace-LiteralOnce $content @'
        public void RemoveMapItem()
        {
            // take the data from list to remove it without having enumeration problems (ToList)
            try
            {
                foreach (var drop in DroppedList.Where(dl => dl.CreatedDate.AddMinutes(1) < DateTime.Now))
                {
                    Broadcast(StaticPacketHelper.Out(UserType.Object, drop.TransportId));
                    DroppedList.Remove(drop.TransportId);
                }
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }
'@ @'
        public void RemoveMapItem()
        {
            // take the data from list to remove it without having enumeration problems (ToList)
            try
            {
                foreach (var drop in DroppedList.Where(dl => dl.CreatedDate.AddMinutes(1) < DateTime.Now))
                {
                    Broadcast(StaticPacketHelper.Out(UserType.Object, drop.TransportId));
                    DroppedList.Remove(drop.TransportId);
                }
            }
            catch (Exception e)
            {
                LogMapException(e);
            }
        }
'@ "expired drop cleanup diagnostics" "public void RemoveMapItem()"

$content = Replace-LiteralOnce $content @'
        internal void StartLife()
        {
            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x =>
            {
                if (InstanceBag?.EndState == 0)
                {
                    foreach (var waveEvent in WaveEvents)
                    {
                        if (waveEvent?.LastStart.AddSeconds(waveEvent.Delay) <= DateTime.Now)
                        {
                            if (waveEvent.Offset == 0 && waveEvent.RunTimes > 0)
                            {
                                waveEvent.Events.ForEach(e => EventHelper.Instance.RunEvent(e));
                                waveEvent.RunTimes--;
                            }

                            waveEvent.Offset = waveEvent.Offset > 0 ? (byte)(waveEvent.Offset - 1) : (byte)0;
                            waveEvent.LastStart = DateTime.Now;
                        }
                    }

                    try
                    {
                        if (!Monsters.Any(s => s.IsAlive && s.Owner?.Character == null && s.Owner?.Mate == null) &&
                            DelayedMonsters.Count == 0)
                        {
                            var OnMapCleanCopy = OnMapClean.ToList();
                            OnMapCleanCopy.ForEach(e => EventHelper.Instance.RunEvent(e));
                            OnMapClean.RemoveAll(s => s != null && OnMapCleanCopy.Contains(s));
                        }

                        if (!IsSleeping)
                        {
                            RemoveMapItem();
                        }
                    }
                    catch (Exception e)
                    {
                        //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
                    }
                }
            });
        }
'@ @'
        internal void StartLife()
        {
            IDisposable mapLifeDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(
                _ =>
                {
                    try
                    {
                        if (InstanceBag?.EndState != 0)
                        {
                            return;
                        }

                        foreach (var waveEvent in WaveEvents)
                        {
                            if (waveEvent?.LastStart.AddSeconds(waveEvent.Delay) <= DateTime.Now)
                            {
                                if (waveEvent.Offset == 0 && waveEvent.RunTimes > 0)
                                {
                                    waveEvent.Events.ForEach(e => EventHelper.Instance.RunEvent(e));
                                    waveEvent.RunTimes--;
                                }

                                waveEvent.Offset = waveEvent.Offset > 0 ? (byte)(waveEvent.Offset - 1) : (byte)0;
                                waveEvent.LastStart = DateTime.Now;
                            }
                        }

                        if (!Monsters.Any(s => s.IsAlive && s.Owner?.Character == null && s.Owner?.Mate == null) &&
                            DelayedMonsters.Count == 0)
                        {
                            var onMapCleanCopy = OnMapClean.ToList();
                            onMapCleanCopy.ForEach(e => EventHelper.Instance.RunEvent(e));
                            OnMapClean.RemoveAll(s => s != null && onMapCleanCopy.Contains(s));
                        }

                        if (!IsSleeping)
                        {
                            RemoveMapItem();
                        }
                    }
                    catch (Exception e)
                    {
                        LogMapException(e);
                    }
                },
                e => LogMapException(e));

            IDisposable previousMapLifeDisposable =
                Interlocked.Exchange(ref _mapLifeDisposable, mapLifeDisposable);
            previousMapLifeDisposable?.Dispose();
        }
'@ "owned map-life subscription" "Interlocked.Exchange(ref _mapLifeDisposable, mapLifeDisposable)"

$content = Replace-LiteralOnce $content @'
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _npcs.Dispose();
'@ @'
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopLife();
                _npcs.Dispose();
'@ "map-life disposal on instance shutdown" "StopLife();$newLine                _npcs.Dispose();"

$content = Replace-LiteralOnce $content @'
                                {
                                    return;
                                }

                                var damage = 0;
'@ @'
                                {
                                    continue;
                                }

                                var damage = 0;
'@ "continue meteorite area processing" "                                    continue;"

if ($content -eq $original) {
    Write-Host "Map lifecycle codemod is already applied."
    exit 0
}

[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $MapInstancePath),
    $content,
    (New-Object Text.UTF8Encoding($true)))

Write-Host "Map lifecycle codemod applied successfully."
