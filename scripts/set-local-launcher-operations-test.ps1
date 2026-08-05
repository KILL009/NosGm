[CmdletBinding(DefaultParameterSetName = "Event")]
param(
    [Parameter(ParameterSetName = "Event")]
    [string]$Title = "Instant Battle de prueba",

    [Parameter(ParameterSetName = "Event")]
    [ValidateSet("instant-battle", "raid", "pvp", "world-boss", "event")]
    [string]$Type = "instant-battle",

    [Parameter(ParameterSetName = "Event")]
    [ValidateSet("pve", "pvp", "social", "special")]
    [string]$Category = "pve",

    [Parameter(ParameterSetName = "Event")]
    [Parameter(ParameterSetName = "Maintenance")]
    [ValidateRange(0, 60)]
    [int]$StartsInMinutes = 2,

    [Parameter(ParameterSetName = "Event")]
    [Parameter(ParameterSetName = "Maintenance")]
    [ValidateRange(2, 180)]
    [int]$DurationMinutes = 10,

    [Parameter(ParameterSetName = "Event")]
    [ValidateRange(0, 255)]
    [int]$Channel = 1,

    [Parameter(ParameterSetName = "Event")]
    [ValidateRange(0, 255)]
    [int]$MinimumLevel = 1,

    [Parameter(ParameterSetName = "Event")]
    [ValidateRange(1, 255)]
    [int]$MaximumLevel = 99,

    [Parameter(ParameterSetName = "Event")]
    [string]$Details = "Evento temporal para comprobar la cuenta regresiva del launcher.",

    [Parameter(ParameterSetName = "Maintenance", Mandatory = $true)]
    [switch]$Maintenance,

    [Parameter(ParameterSetName = "Maintenance")]
    [string]$MaintenanceTitle = "Mantenimiento de prueba",

    [Parameter(ParameterSetName = "Maintenance")]
    [string]$MaintenanceMessage = "Ventana temporal para comprobar la advertencia del launcher.",

    [Parameter(ParameterSetName = "Clear", Mandatory = $true)]
    [switch]$Clear
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$directory = Join-Path $root "artifacts\modern-login-local\public-data"
$path = Join-Path $directory "public-events.json"

function Limit-Text([string]$Value, [int]$MaximumLength) {
    $clean = -join @($Value.ToCharArray() | Where-Object { -not [char]::IsControl($_) })
    $clean = $clean.Trim()
    if ($clean.Length -gt $MaximumLength) {
        return $clean.Substring(0, $MaximumLength)
    }
    return $clean
}

function Write-AtomicJson([object]$Document) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporaryPath = $path + "." + [Guid]::NewGuid().ToString("N") + ".tmp"
    try {
        $json = $Document | ConvertTo-Json -Depth 8
        [IO.File]::WriteAllText(
            $temporaryPath,
            $json,
            (New-Object Text.UTF8Encoding($false)))
        Move-Item -LiteralPath $temporaryPath -Destination $path -Force
    }
    finally {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
}

if ($MinimumLevel -gt $MaximumLevel) {
    throw "MinimumLevel cannot be greater than MaximumLevel."
}

if ($Clear) {
    Write-AtomicJson ([ordered]@{
        maintenance = $null
        events = @()
    })
    Write-Host "[DONE] Launcher operations test data cleared: $path" -ForegroundColor Green
    return
}

$now = [DateTimeOffset]::Now
$startsAt = $now.AddMinutes($StartsInMinutes)
$endsAt = $startsAt.AddMinutes($DurationMinutes)

if ($Maintenance) {
    Write-AtomicJson ([ordered]@{
        maintenance = [ordered]@{
            title = Limit-Text $MaintenanceTitle 100
            message = Limit-Text $MaintenanceMessage 400
            startsAt = $startsAt.ToString("o")
            endsAt = $endsAt.ToString("o")
        }
        events = @()
    })

    Write-Host "[DONE] Maintenance test written: $path" -ForegroundColor Green
    Write-Host "Starts: $($startsAt.ToString('dd/MM/yyyy HH:mm:ss zzz'))"
    Write-Host "Ends:   $($endsAt.ToString('dd/MM/yyyy HH:mm:ss zzz'))"
    Write-Host "World republishes operations within about 15 seconds."
    return
}

$eventId = "test-" + $Type + "-" + $startsAt.ToUniversalTime().ToString("yyyyMMddHHmmss")
Write-AtomicJson ([ordered]@{
    maintenance = $null
    events = @(
        [ordered]@{
            id = $eventId
            type = $Type
            title = Limit-Text $Title 120
            category = $Category
            startsAt = $startsAt.ToString("o")
            endsAt = $endsAt.ToString("o")
            channel = $Channel
            minimumLevel = $MinimumLevel
            maximumLevel = $MaximumLevel
            details = Limit-Text $Details 400
        }
    )
})

Write-Host "[DONE] Event countdown test written: $path" -ForegroundColor Green
Write-Host "Starts: $($startsAt.ToString('dd/MM/yyyy HH:mm:ss zzz'))"
Write-Host "Ends:   $($endsAt.ToString('dd/MM/yyyy HH:mm:ss zzz'))"
Write-Host "World republishes operations within about 15 seconds."
