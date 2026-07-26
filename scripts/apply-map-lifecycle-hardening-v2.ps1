param(
    [string]$Path = "Data/NosGm.GameObject/Map/MapInstance.cs"
)

$ErrorActionPreference = "Stop"

function Replace-Exact {
    param(
        [string]$InputText,
        [string]$OldText,
        [string]$NewText,
        [string]$Description,
        [int]$ExpectedCount = 1
    )

    $count = [System.Text.RegularExpressions.Regex]::Matches(
        $InputText,
        [System.Text.RegularExpressions.Regex]::Escape($OldText)).Count

    if ($count -ne $ExpectedCount) {
        throw "Unexpected match count for ${Description}: expected $ExpectedCount, found $count"
    }

    return $InputText.Replace($OldText, $NewText)
}

function Replace-Regex {
    param(
        [string]$InputText,
        [string]$Pattern,
        [string]$Replacement,
        [string]$Description,
        [int]$ExpectedCount = 1
    )

    $matches = [System.Text.RegularExpressions.Regex]::Matches(
        $InputText,
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Multiline)

    if ($matches.Count -ne $ExpectedCount) {
        throw "Unexpected match count for ${Description}: expected $ExpectedCount, found $($matches.Count)"
    }

    return [System.Text.RegularExpressions.Regex]::Replace(
        $InputText,
        $Pattern,
        $Replacement,
        [System.Text.RegularExpressions.RegexOptions]::Multiline)
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "MapInstance source not found: $Path"
}

$content = [System.IO.File]::ReadAllText($Path)
$content = [System.Text.RegularExpressions.Regex]::Replace($content, "\r\n|\r", "`n")

$content = Replace-Exact $content @'
using System.Reactive.Linq;
'@ @'
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
'@ "caller member import"

$content = Replace-Exact $content @'
        private readonly Random _random;
'@ @'
        private readonly Random _random;

        private static readonly long MapDiagnosticIntervalTicks = TimeSpan.FromSeconds(30).Ticks;

        private readonly ConcurrentDictionary<string, long> _lastDiagnosticLogTicks =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
'@ "map diagnostic fields"

$content = Replace-Exact $content @'
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
'@ "map diagnostic helper"

$content = Replace-Exact $content @'
//LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
'@ @'
LogMapException(e);
'@ "swallowed map exceptions" 3

$content = Replace-Regex $content `
    '^(\s*)mapMonster\.Initialize\(this\);\n\1mapMonster\.Initialize\(this\);' `
    '$1mapMonster.Initialize(this);' `
    "duplicate monster initialization"

$content = Replace-Exact $content @'
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
Write-Host "Map lifecycle hardening v2 applied."
