$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

$requiredFiles = @(
    'Tools/NosGM.DataUpdater/NosGM.DataUpdater.csproj',
    'Tools/NosGM.DataUpdater/Program.cs',
    'Tools/NosGM.DataUpdater/UpdaterOptions.cs',
    'Tools/NosGM.DataUpdater/Extraction/BCardCatalogExtractor.cs',
    'Tools/NosGM.DataUpdater/Publishing/GitHubPullRequestPublisher.cs',
    'Tools/NosGM.DataUpdater/README.md',
    'Tools/NosGM.DataUpdater/NOTICE.md',
    '.github/workflows/update-bcard-catalogs.yml',
    'Data/Generated/BCards/README.md'
)

foreach ($path in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail "Required NosGM.DataUpdater file is missing: $path"
    }
}

$upstreamCommit = '53153c990ae5b65a603d223eeda504df2a67d5fb'
$notice = Get-Content -LiteralPath 'Tools/NosGM.DataUpdater/NOTICE.md' -Raw
if (-not $notice.Contains('noszanou/BCardGistUpdater') -or -not $notice.Contains($upstreamCommit)) {
    Fail 'NosGM.DataUpdater NOTICE.md must preserve the BCardGistUpdater repository and immutable upstream commit.'
}

$project = Get-Content -LiteralPath 'Tools/NosGM.DataUpdater/NosGM.DataUpdater.csproj' -Raw
foreach ($requiredText in @('GPL-3.0-only', 'noszanou', 'BCardGistUpdater contributors', 'NosGM contributors')) {
    if (-not $project.Contains($requiredText)) {
        Fail "NosGM.DataUpdater.csproj is missing attribution or license text: $requiredText"
    }
}

$sourceFiles = @(Get-ChildItem -LiteralPath 'Tools/NosGM.DataUpdater' -Filter '*.cs' -File -Recurse)
foreach ($sourceFile in $sourceFiles) {
    $source = Get-Content -LiteralPath $sourceFile.FullName -Raw
    if (-not $source.Contains('SPDX-License-Identifier: GPL-3.0-only')) {
        Fail "$($sourceFile.FullName) is missing the GPL-3.0-only SPDX notice."
    }

    if (-not $source.Contains('noszanou') -or -not $source.Contains('NosGM contributors')) {
        Fail "$($sourceFile.FullName) is missing upstream or NosGM attribution."
    }
}

$publisher = Get-Content -LiteralPath 'Tools/NosGM.DataUpdater/Publishing/GitHubPullRequestPublisher.cs' -Raw
if ($publisher.Contains('GistId') -or $publisher.Contains('api.github.com/gists')) {
    Fail 'NosGM.DataUpdater must not retain the original hard-coded Gist publisher.'
}

$noChangesIndex = $publisher.IndexOf('if (!plan.HasChanges)', [System.StringComparison]::Ordinal)
$createBranchIndex = $publisher.IndexOf('CreateBranchAsync', [System.StringComparison]::Ordinal)
if ($noChangesIndex -lt 0 -or $createBranchIndex -lt 0 -or $noChangesIndex -gt $createBranchIndex) {
    Fail 'The publisher must check for actual changes before creating a GitHub branch.'
}

$options = Get-Content -LiteralPath 'Tools/NosGM.DataUpdater/UpdaterOptions.cs' -Raw
foreach ($language in @('ES', 'EN', 'DE', 'FR', 'IT', 'PL', 'CZ', 'RU', 'JP', 'CN')) {
    if (-not $options.Contains('"' + $language + '"')) {
        Fail "Default language list is missing $language."
    }
}

$workflow = Get-Content -LiteralPath '.github/workflows/update-bcard-catalogs.yml' -Raw
foreach ($requiredWorkflowText in @(
    'secrets.NOSGAME_PACKAGE_TOKEN',
    'github.token',
    'pull-requests: write',
    '--publish'
)) {
    if (-not $workflow.Contains($requiredWorkflowText)) {
        Fail "BCard updater workflow is missing required safety configuration: $requiredWorkflowText"
    }
}

$forbiddenTrackedFiles = @(git ls-files 'Data/Generated/BCards' | Where-Object {
    $_ -notmatch '(README\.md|BCard_[A-Z]+\.json|manifest\.json|CHANGE_REPORT\.md)$'
})
if ($forbiddenTrackedFiles.Count -gt 0) {
    $forbiddenTrackedFiles | ForEach-Object { Write-Host "forbidden: $_" }
    Fail 'Generated BCard directory contains an unexpected client or binary asset.'
}

Write-Host 'NosGM.DataUpdater safety and attribution checks passed.'
