// SPDX-License-Identifier: MIT

namespace NosGM.Web.Contracts;

public sealed record PublicRateMultiplier(
    string Id,
    string Name,
    int Multiplier);

public sealed record PublicMaintenanceStatus(
    bool IsActive,
    string Title,
    string Message,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

public sealed record PublicCalendarEvent(
    string Id,
    string Type,
    string Title,
    string Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Channel,
    int MinimumLevel,
    int MaximumLevel,
    string Details);

public sealed record PublicOperationsSnapshot(
    DateTimeOffset ObservedAt,
    IReadOnlyList<PublicRateMultiplier> Rates,
    PublicMaintenanceStatus Maintenance,
    IReadOnlyList<PublicCalendarEvent> Events,
    bool IsStale = false);
