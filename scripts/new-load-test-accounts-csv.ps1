[CmdletBinding()]
param(
    [ValidateRange(1, 9999)]
    [int]$Count = 1000,
    [string]$Prefix = "load",
    [string]$Password = "NosGM_Load_2026!",
    [string]$Path = "C:\NosGM-Test\accounts.csv"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($Prefix)) {
    throw "Prefix cannot be empty."
}

if ($Prefix.Contains(",") -or $Password.Contains(",")) {
    throw "Prefix and password cannot contain commas because NosGM.LoadTest uses a simple CSV reader."
}

$directory = Split-Path -Parent $Path
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add("username,password,slot")

for ($index = 1; $index -le $Count; $index++) {
    $username = "{0}{1:D4}" -f $Prefix, $index
    $lines.Add("$username,$Password,0")
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines([System.IO.Path]::GetFullPath($Path), $lines, $utf8NoBom)

Write-Host "[LOAD] Wrote $Count accounts to $([System.IO.Path]::GetFullPath($Path))" -ForegroundColor Green
Write-Host "[LOAD] Users: $($Prefix)0001 .. $($Prefix)$($Count.ToString('D4')) | slot=0" -ForegroundColor Cyan
