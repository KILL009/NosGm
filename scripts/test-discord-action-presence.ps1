param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$worldPath = Join-Path $root "Data/NosGm.Program/NosGm.World/Properties/AssemblyInfo.cs"
if (-not (Test-Path -LiteralPath $worldPath -PathType Leaf)) {
    throw "World presence source was not found."
}

$world = Get-Content -LiteralPath $worldPath -Raw
$marker = "    internal sealed class LauncherPresenceAction"
$start = $world.IndexOf($marker, [StringComparison]::Ordinal)
if ($start -lt 0) {
    throw "Action presence classifier source was not found."
}

# The action classes are intentionally the final members of the NosGm.World
# namespace. Their source therefore includes the original namespace closing
# brace and can be compiled inside a fresh namespace declaration below.
$actionSource = $world.Substring($start)
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) (
    "NosGM-PresenceAction-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $tempRoot "PresenceAction.SelfTest.csproj") -Encoding utf8

    @'
using System;

namespace NosGm.GameObject
{
    internal sealed class Character
    {
        public bool IsFishing { get; set; }
        public short CurrentMinigame { get; set; }
        public object ExchangeInfo { get; set; }
        public bool IsShopping { get; set; }
        public DateTime LastSkillUse { get; set; }
        public DateTime LastDefence { get; set; }
        public DateTime LastMove { get; set; }
        public DateTime LastMessage { get; set; }
        public DateTime LastCommand { get; set; }
        public DateTime LastFishBite { get; set; }
        public DateTime LastFishCycle { get; set; }
    }
}
'@ | Set-Content -LiteralPath (Join-Path $tempRoot "CharacterStub.cs") -Encoding utf8

    $classifier = @"
using System;
using NosGm.GameObject;

namespace NosGm.World
{
$actionSource
"@
    $classifier | Set-Content -LiteralPath (Join-Path $tempRoot "LauncherPresenceActionClassifier.cs") -Encoding utf8

    @'
using System;
using NosGm.GameObject;

namespace NosGm.World
{
    internal static class Program
    {
        private static readonly DateTime Now = new DateTime(
            2026,
            8,
            6,
            12,
            0,
            0,
            DateTimeKind.Local);

        private static int Main()
        {
            AssertResult(
                "fallback",
                new Character(),
                Now,
                "raid",
                "Participando en una raid");

            AssertResult(
                "fishing priority",
                new Character
                {
                    IsFishing = true,
                    CurrentMinigame = 3,
                    ExchangeInfo = new object(),
                    IsShopping = true,
                    LastSkillUse = Now.AddSeconds(-1)
                },
                Now.AddMinutes(-10),
                "fishing",
                "Pescando en Prados Soleados");

            AssertResult(
                "minigame priority",
                new Character
                {
                    CurrentMinigame = 1,
                    ExchangeInfo = new object(),
                    IsShopping = true,
                    LastSkillUse = Now.AddSeconds(-1)
                },
                Now.AddMinutes(-10),
                "minigame",
                "Participando en un minijuego");

            AssertResult(
                "exchange priority",
                new Character
                {
                    ExchangeInfo = new object(),
                    IsShopping = true,
                    LastSkillUse = Now.AddSeconds(-1)
                },
                Now.AddMinutes(-10),
                "trading",
                "Intercambiando objetos");

            AssertResult(
                "shop priority",
                new Character
                {
                    IsShopping = true,
                    LastSkillUse = Now.AddSeconds(-1)
                },
                Now.AddMinutes(-10),
                "shopping",
                "Revisando una tienda");

            AssertResult(
                "recent skill combat",
                new Character { LastSkillUse = Now.AddSeconds(-5) },
                Now,
                "combat",
                "Combatiendo en Prados Soleados");

            AssertResult(
                "recent defence combat",
                new Character { LastDefence = Now.AddSeconds(-15) },
                Now,
                "combat",
                "Combatiendo en Prados Soleados");

            AssertResult(
                "combat expiry",
                new Character { LastSkillUse = Now.AddSeconds(-16) },
                Now,
                "raid",
                "Participando en una raid");

            AssertResult(
                "afk threshold",
                new Character(),
                Now.AddMinutes(-5),
                "afk",
                "Ausente en Prados Soleados");

            AssertResult(
                "movement clears afk",
                new Character { LastMove = Now.AddSeconds(-1) },
                Now.AddMinutes(-10),
                "raid",
                "Participando en una raid");

            AssertResult(
                "message clears afk",
                new Character { LastMessage = Now.AddMinutes(-1) },
                Now.AddMinutes(-10),
                "raid",
                "Participando en una raid");

            AssertResult(
                "future combat ignored",
                new Character { LastSkillUse = Now.AddMinutes(2) },
                Now,
                "raid",
                "Participando en una raid");

            LauncherPresenceAction safeMap = LauncherPresenceActionClassifier.Resolve(
                new Character { IsFishing = true },
                "   ",
                "playing",
                "Jugando en NosGM",
                Now,
                Now);
            AssertEqual("safe map activity", "fishing", safeMap.Activity);
            AssertEqual("safe map details", "Pescando en Sumeria", safeMap.Details);

            Console.WriteLine(
                "NosGM Discord action presence runtime self-test passed.");
            return 0;
        }

        private static void AssertResult(
            string name,
            Character character,
            DateTime registeredAt,
            string expectedActivity,
            string expectedDetails)
        {
            LauncherPresenceAction result = LauncherPresenceActionClassifier.Resolve(
                character,
                "Prados Soleados",
                "raid",
                "Participando en una raid",
                registeredAt,
                Now);
            AssertEqual(name + " activity", expectedActivity, result.Activity);
            AssertEqual(name + " details", expectedDetails, result.Details);
        }

        private static void AssertEqual(
            string name,
            string expected,
            string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    name + " failed. Expected '" + expected +
                    "' but received '" + actual + "'.");
            }

            Console.WriteLine("[PASS] " + name);
        }
    }
}
'@ | Set-Content -LiteralPath (Join-Path $tempRoot "Program.cs") -Encoding utf8

    $previousNativePreference = $PSNativeCommandUseErrorActionPreference
    $PSNativeCommandUseErrorActionPreference = $false
    try {
        & dotnet run `
            --project (Join-Path $tempRoot "PresenceAction.SelfTest.csproj") `
            --configuration Release `
            --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Discord action presence runtime self-test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        $PSNativeCommandUseErrorActionPreference = $previousNativePreference
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
