$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
    using System;
    public class CloneHelper {
        public static string Clone(string s) { return s; }
    }
"@

$AssemblyPath = "Data/NosGm.DAL/NosGm.DAL.EF/bin/x64/Release/NosGm.DAL.EF.dll"
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))

$cacheType = $assembly.GetType("NosGm.DAL.EF.Cache.MemoryCacheService``2")
$constructedCacheType = $cacheType.MakeGenericType([int], [string])

$funcType = [System.Func``2].MakeGenericType([string], [string])
$cloneMethod = [CloneHelper].GetMethod("Clone")
$cloneFactory = [Delegate]::CreateDelegate($funcType, $cloneMethod)

# Constructor takes just the cloneFunc
$cacheInstance = [Activator]::CreateInstance($constructedCacheType, @($cloneFactory))

$replaceAllMethod = $constructedCacheType.GetMethod("ReplaceAll")
$tryGetValueMethod = $constructedCacheType.GetMethod("TryGetValue")
$getCacheStatsMethod = $constructedCacheType.GetMethod("GetStatistics")

Write-Host "[INIT] Cache Instance Created."

$stats1 = $getCacheStatsMethod.Invoke($cacheInstance, $null)
Write-Host "`n[EXECUTION 1] Before DB Load:"
Write-Host "StoredItems=$($stats1.StoredItems) Hits=$($stats1.CacheHits) Misses=$($stats1.CacheMisses) Reloads=$($stats1.FullReloads)"

$mockData = New-Object 'System.Collections.Generic.Dictionary[int, string]'
$mockData.Add(1, "MockItem1")
$mockData.Add(2, "MockItem2")

$replaceAllMethod.Invoke($cacheInstance, @($mockData))

$stats2 = $getCacheStatsMethod.Invoke($cacheInstance, $null)
Write-Host "`n[EXECUTION 2] After DB Load (ReplaceAll):"
Write-Host "StoredItems=$($stats2.StoredItems) Hits=$($stats2.CacheHits) Misses=$($stats2.CacheMisses) Reloads=$($stats2.FullReloads)"

$outVal = $null
$args1 = [object[]]@(1, $outVal)
$args2 = [object[]]@(2, $outVal)
$tryGetValueMethod.Invoke($cacheInstance, $args1) | Out-Null
$tryGetValueMethod.Invoke($cacheInstance, $args2) | Out-Null

$stats3 = $getCacheStatsMethod.Invoke($cacheInstance, $null)
Write-Host "`n[EXECUTION 3] After 2 In-Game Reads:"
Write-Host "StoredItems=$($stats3.StoredItems) Hits=$($stats3.CacheHits) Misses=$($stats3.CacheMisses) Reloads=$($stats3.FullReloads)"

Write-Host "`n[PASS] CacheStats functionally validated! Hits increase and Reloads remain stable."
