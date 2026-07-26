param(
    [string]$Path = "Data/NosGm.GameObject/Map/MapInstance.cs"
)

$ErrorActionPreference = "Stop"

function Replace-Required {
    param(
        [string]$InputText,
        [string]$OldText,
        [string]$NewText,
        [string]$Description
    )

    if (-not $InputText.Contains($OldText)) {
        throw "Required source block was not found: $Description"
    }

    return $InputText.Replace($OldText, $NewText)
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "MapInstance source not found: $Path"
}

$content = [System.IO.File]::ReadAllText($Path)
$content = [System.Text.RegularExpressions.Regex]::Replace($content, "\r\n|\r", "`n")

$content = Replace-Required $content @'
        private readonly Random _random;

        private IDisposable _mapLifeDisposable;
'@ @'
        private readonly Random _random;

        private static readonly long MapDiagnosticIntervalTicks = TimeSpan.FromSeconds(30).Ticks;

        private readonly ConcurrentDictionary<string, long> _lastDiagnosticLogTicks =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);

        private IDisposable _mapLifeDisposable;
'@ "map diagnostic state"

$content = Replace-Required $content @'
        public void StopLife()
        {
            _mapLifeDisposable?.Dispose();
        }

        public void AddDelayedMonster
'@ @'
        public void StopLife()
        {
            _mapLifeDisposable?.Dispose();
        }

        private void LogMapException(string operation, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(operation) || exception == null)
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

        public void AddDelayedMonster
'@ "throttled map diagnostics helper"

$content = Replace-Required $content @'
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }

        public void DropItems
'@ @'
            catch (Exception e)
            {
                LogMapException(nameof(DropItemByMonster), e);
            }
        }

        public void DropItems
'@ "drop creation diagnostics"

$content = Replace-Required $content @'
                mapMonster.Initialize(this);
                mapMonster.Initialize(this);
                var mapMonsterId = mapMonster.MapMonsterId;
'@ @'
                mapMonster.Initialize(this);
                var mapMonsterId = mapMonster.MapMonsterId;
'@ "duplicate monster initialization"

$content = Replace-Required $content @'
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }

        public void RemoveMonster
'@ @'
            catch (Exception e)
            {
                LogMapException(nameof(RemoveMapItem), e);
            }
        }

        public void RemoveMonster
'@ "drop cleanup diagnostics"

$content = Replace-Required $content @'
                    catch (Exception e)
                    {
                        //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
                    }
                }
            });
        }

        internal int SummonMonster
'@ @'
                    catch (Exception e)
                    {
                        LogMapException(nameof(StartLife), e);
                    }
                }
            });
        }

        internal int SummonMonster
'@ "map lifecycle diagnostics"

$content = Replace-Required $content @'
                                if (monsterToSummon == null || x.Mate != null || x.MapNpc != null || x.MapMonster?.IsBoss == true
                                    || (x.Character != null && x.Character.CharacterId == mapMonster.Owner?.MapEntityId)
                                    || (x.MapMonster != null && monsterToSummon.Owner == null))
                                {
                                    return;
                                }
'@ @'
                                if (monsterToSummon == null || x.Mate != null || x.MapNpc != null || x.MapMonster?.IsBoss == true
                                    || (x.Character != null && x.Character.CharacterId == mapMonster.Owner?.MapEntityId)
                                    || (x.MapMonster != null && monsterToSummon.Owner == null))
                                {
                                    continue;
                                }
'@ "meteorite target iteration"

$content = $content.Replace("`n", [Environment]::NewLine)
[System.IO.File]::WriteAllText($Path, $content, [System.Text.UTF8Encoding]::new($true))
Write-Host "Map lifecycle hardening applied."
