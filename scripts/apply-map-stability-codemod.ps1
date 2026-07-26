param(
    [string]$MapInstancePath = "Data/NosGm.GameObject/Map/MapInstance.cs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $MapInstancePath)) {
    throw "MapInstance source not found: $MapInstancePath"
}

$content = Get-Content -LiteralPath $MapInstancePath -Raw
$original = $content

function Replace-LiteralOnce {
    param(
        [string]$Text,
        [string]$OldValue,
        [string]$NewValue,
        [string]$Description,
        [string]$AppliedMarker
    )

    if ($Text.Contains($AppliedMarker)) {
        return $Text
    }

    $first = $Text.IndexOf($OldValue, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Unable to find expected source for: $Description"
    }

    $second = $Text.IndexOf($OldValue, $first + $OldValue.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match for: $Description"
    }

    return $Text.Substring(0, $first) + $NewValue + $Text.Substring($first + $OldValue.Length)
}

function Replace-RegexOnce {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Replacement,
        [string]$Description,
        [string]$AppliedMarker
    )

    if ($Text.Contains($AppliedMarker)) {
        return $Text
    }

    $matches = [regex]::Matches($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Singleline)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one regex match for '$Description', found $($matches.Count)."
    }

    return [regex]::Replace(
        $Text,
        $Pattern,
        [Text.RegularExpressions.MatchEvaluator]{ param($match) $Replacement },
        [Text.RegularExpressions.RegexOptions]::Singleline)
}

$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

$memberMarker = "        public ConcurrentBag<MapDesignObject> MapDesignObjects = new ConcurrentBag<MapDesignObject>();"
$memberReplacement = @(
    $memberMarker,
    "",
    "        private static readonly TimeSpan MapDiagnosticThrottle = TimeSpan.FromMinutes(1);",
    "",
    "        private readonly ConcurrentDictionary<string, DateTime> _mapDiagnosticLastLogged =",
    "            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);"
) -join $newLine
$content = Replace-LiteralOnce $content $memberMarker $memberReplacement `
    "bounded map diagnostics fields" "MapDiagnosticThrottle = TimeSpan.FromMinutes(1)"

$oldStopLife = @(
    "        public void StopLife()",
    "        {",
    "            _mapLifeDisposable?.Dispose();",
    "        }"
) -join $newLine
$newStopLife = @(
    "        public void StopLife()",
    "        {",
    "            IDisposable mapLifeDisposable = _mapLifeDisposable;",
    "            _mapLifeDisposable = null;",
    "            mapLifeDisposable?.Dispose();",
    "        }"
) -join $newLine
$content = Replace-LiteralOnce $content $oldStopLife $newStopLife `
    "disposable map life shutdown" "IDisposable mapLifeDisposable = _mapLifeDisposable;"

$oldDropCatch = @(
    "            catch (Exception e)",
    "            {",
    "                //LOGGERServerLog($\"{e.ToString()}\", LogType.ServerError);",
    "            }"
) -join $newLine
$newDropCatch = @(
    "            catch (Exception e)",
    "            {",
    "                LogMapOperationFailure(",
    "                    \"DropItemByMonster\",",
    "                    e,",
    "                    $\"ItemVNum={drop?.ItemVNum ?? 0} Owner={owner ?? -1} X={mapX} Y={mapY}\");",
    "            }"
) -join $newLine
$content = Replace-LiteralOnce $content $oldDropCatch $newDropCatch `
    "monster drop diagnostics" "\"DropItemByMonster\","

$oldMonsterLoad = @(
    "            foreach (var monster in monsters)",
    "            {",
    "                var tmp = new MapMonster(monster);",
    "                if (!(tmp is MapMonster mapMonster))",
    "                {",
    "                    return;",
    "                }",
    "",
    "                mapMonster.Initialize(this);",
    "                mapMonster.Initialize(this);",
    "                var mapMonsterId = mapMonster.MapMonsterId;"
) -join $newLine
$newMonsterLoad = @(
    "            foreach (var monster in monsters)",
    "            {",
    "                var mapMonster = new MapMonster(monster);",
    "                mapMonster.Initialize(this);",
    "                var mapMonsterId = mapMonster.MapMonsterId;"
) -join $newLine
$content = Replace-LiteralOnce $content $oldMonsterLoad $newMonsterLoad `
    "single monster initialization" "var mapMonster = new MapMonster(monster);"

$oldRemoveCatch = @(
    "            catch (Exception e)",
    "            {",
    "                //LOGGERServerLog($\"{e.ToString()}\", LogType.ServerError);",
    "            }"
) -join $newLine
$newRemoveCatch = @(
    "            catch (Exception e)",
    "            {",
    "                LogMapOperationFailure(\"RemoveMapItem\", e, $\"DropCount={DroppedList.Count}\");",
    "            }"
) -join $newLine
$content = Replace-LiteralOnce $content $oldRemoveCatch $newRemoveCatch `
    "expired drop cleanup diagnostics" "LogMapOperationFailure(\"RemoveMapItem\""

$helperMarker = "        internal void CreatePortal(Portal portal)"
$helper = @(
    "        private void LogMapOperationFailure(string operation, Exception exception, string context = null)",
    "        {",
    "            if (exception == null)",
    "            {",
    "                return;",
    "            }",
    "",
    "            DateTime now = DateTime.UtcNow;",
    "            while (true)",
    "            {",
    "                if (_mapDiagnosticLastLogged.TryGetValue(operation, out DateTime lastLogged))",
    "                {",
    "                    if (now - lastLogged < MapDiagnosticThrottle)",
    "                    {",
    "                        return;",
    "                    }",
    "",
    "                    if (!_mapDiagnosticLastLogged.TryUpdate(operation, now, lastLogged))",
    "                    {",
    "                        continue;",
    "                    }",
    "                }",
    "                else if (!_mapDiagnosticLastLogged.TryAdd(operation, now))",
    "                {",
    "                    continue;",
    "                }",
    "",
    "                break;",
    "            }",
    "",
    "            string suffix = string.IsNullOrWhiteSpace(context) ? string.Empty : $\" {context}\";",
    "            Logger.Error(",
    "                $\"[MAP_OPERATION_FAILED] Operation={operation} MapId={Map?.MapId ?? 0} \" +",
    "                $\"MapInstanceId={MapInstanceId}{suffix}\",",
    "                exception);",
    "        }",
    "",
    $helperMarker
) -join $newLine
$content = Replace-LiteralOnce $content $helperMarker $helper `
    "throttled map failure logger" "private void LogMapOperationFailure(string operation"

$startLifePattern = "        internal void StartLife\(\)\r?\n        \{.*?\r?\n        \}\r?\n\r?\n        internal int SummonMonster"
$newStartLife = @(
    "        internal void StartLife()",
    "        {",
    "            StopLife();",
    "",
    "            _mapLifeDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(",
    "                _ =>",
    "                {",
    "                    try",
    "                    {",
    "                        if (InstanceBag?.EndState != 0)",
    "                        {",
    "                            return;",
    "                        }",
    "",
    "                        foreach (var waveEvent in WaveEvents)",
    "                        {",
    "                            if (waveEvent?.LastStart.AddSeconds(waveEvent.Delay) <= DateTime.Now)",
    "                            {",
    "                                if (waveEvent.Offset == 0 && waveEvent.RunTimes > 0)",
    "                                {",
    "                                    waveEvent.Events.ForEach(e => EventHelper.Instance.RunEvent(e));",
    "                                    waveEvent.RunTimes--;",
    "                                }",
    "",
    "                                waveEvent.Offset = waveEvent.Offset > 0 ? (byte)(waveEvent.Offset - 1) : (byte)0;",
    "                                waveEvent.LastStart = DateTime.Now;",
    "                            }",
    "                        }",
    "",
    "                        if (!Monsters.Any(s => s.IsAlive && s.Owner?.Character == null && s.Owner?.Mate == null) &&",
    "                            DelayedMonsters.Count == 0)",
    "                        {",
    "                            var onMapCleanCopy = OnMapClean.ToList();",
    "                            onMapCleanCopy.ForEach(e => EventHelper.Instance.RunEvent(e));",
    "                            OnMapClean.RemoveAll(s => s != null && onMapCleanCopy.Contains(s));",
    "                        }",
    "",
    "                        if (!IsSleeping)",
    "                        {",
    "                            RemoveMapItem();",
    "                        }",
    "                    }",
    "                    catch (Exception e)",
    "                    {",
    "                        LogMapOperationFailure(",
    "                            \"MapLifeTick\",",
    "                            e,",
    "                            $\"WaveEvents={WaveEvents?.Count ?? 0} Monsters={_monsters.Count} Delayed={_delayedMonsters.Count}\");",
    "                    }",
    "                },",
    "                e => LogMapOperationFailure(\"MapLifeSubscription\", e));",
    "        }",
    "",
    "        internal int SummonMonster"
) -join $newLine
$content = Replace-RegexOnce $content $startLifePattern $newStartLife `
    "owned and observable map life subscription" "_mapLifeDisposable = Observable.Interval"

$oldMeteoriteGuard = @(
    "                                if (monsterToSummon == null || x.Mate != null || x.MapNpc != null || x.MapMonster?.IsBoss == true",
    "                                    || (x.Character != null && x.Character.CharacterId == mapMonster.Owner?.MapEntityId)",
    "                                    || (x.MapMonster != null && monsterToSummon.Owner == null))",
    "                                {",
    "                                    return;",
    "                                }"
) -join $newLine
$newMeteoriteGuard = @(
    "                                if (monsterToSummon == null || x.Mate != null || x.MapNpc != null || x.MapMonster?.IsBoss == true",
    "                                    || (x.Character != null && x.Character.CharacterId == mapMonster.Owner?.MapEntityId)",
    "                                    || (x.MapMonster != null && monsterToSummon.Owner == null))",
    "                                {",
    "                                    continue;",
    "                                }"
) -join $newLine
$content = Replace-LiteralOnce $content $oldMeteoriteGuard $newMeteoriteGuard `
    "meteorite target continuation" "                                    continue;"

if ($content -eq $original) {
    Write-Host "Map stability codemod is already applied."
    exit 0
}

[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $MapInstancePath),
    $content,
    (New-Object Text.UTF8Encoding($true)))

Write-Host "Map stability codemod applied successfully."
