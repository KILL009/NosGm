param(
    [Parameter(Mandatory = $false)]
    [string]$Path = "diagnostics"
)

$logs = Get-ChildItem $Path -Filter *.log -Recurse | Sort-Object Name
if (-not $logs) {
    Write-Error "No MSBuild text logs were found under '$Path'."
    exit 1
}

$found = $false
foreach ($log in $logs) {
    $matches = Select-String -Path $log.FullName -Pattern "error [A-Z]+[0-9]+|error MSB[0-9]+|MSB[0-9]+:|ResX|resheader" -Context 1,2
    if ($matches) {
        $found = $true
        Write-Host "=== $($log.Name) ==="
        $matches | Select-Object -First 40 | ForEach-Object { Write-Host $_.ToString() }
    }
}

if (-not $found) {
    Write-Host "No standard error pattern was found. Showing the last 80 lines of each log."
    foreach ($log in $logs) {
        Write-Host "=== $($log.Name) ==="
        Get-Content $log.FullName -Tail 80
    }
}

exit 1
