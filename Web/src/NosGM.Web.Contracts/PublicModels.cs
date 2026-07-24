// SPDX-License-Identifier: MIT

namespace NosGM.Web.Contracts;

public enum ServiceHealth
{
    Offline = 0,
    Degraded = 1,
    Online = 2
}

public enum RankingKind
{
    Combat = 0,
    Reputation = 1,
    Hero = 2
}

public sealed record PublicNewsItem(
    string Id,
    string Slug,
    string Title,
    string Summary,
    DateTimeOffset PublishedAt);

public sealed record PublicServiceStatus(
    string Id,
    string Name,
    ServiceHealth Health,
    int OnlinePlayers);

public sealed record PublicServerStatus(
    string ServerName,
    ServiceHealth OverallHealth,
    int OnlinePlayers,
    IReadOnlyList<PublicServiceStatus> Services,
    DateTimeOffset ObservedAt);

public sealed record PublicRankingEntry(
    int Position,
    string CharacterName,
    int Level,
    int HeroLevel,
    long Reputation);

public sealed record PublicPortalMetadata(
    string ServerName,
    string ClientVersion,
    bool LauncherDownloadAvailable,
    IReadOnlyList<string> SupportedLanguages);
