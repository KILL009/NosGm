// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal sealed record LauncherPresenceState
{
    public int SchemaVersion { get; init; } = 1;
    public string Activity { get; init; } = "playing";
    public string Details { get; init; } = "Jugando en NosGM";
    public string MapName { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public int Level { get; init; }
    public int HeroLevel { get; init; }
    public string ClassName { get; init; } = string.Empty;
    public int ChannelId { get; init; }
    public int PartyCurrent { get; init; }
    public int PartyMaximum { get; init; }
    public long SessionStartedUnixSeconds { get; init; }
    public string LargeImageKey { get; init; } = "nosgm";
    public string LargeImageText { get; init; } = "NosGM";
    public string SmallImageKey { get; init; } = string.Empty;
    public string SmallImageText { get; init; } = string.Empty;

    public DiscordPresenceActivity ToDiscordActivity(
        LauncherSettings settings,
        long fallbackStartedAtUnixSeconds)
    {
        var details = settings.DiscordShowMap &&
                      !string.IsNullOrWhiteSpace(MapName)
            ? Details
            : GenericActivityText(Activity);

        var stateParts = new List<string>();
        if (settings.DiscordShowCharacterName &&
            !string.IsNullOrWhiteSpace(CharacterName))
        {
            stateParts.Add(CharacterName.Trim());
        }

        if (Level > 0)
        {
            stateParts.Add(HeroLevel > 0
                ? $"Nv. {Level} +{HeroLevel}"
                : $"Nv. {Level}");
        }

        if (settings.DiscordShowChannel && ChannelId > 0)
        {
            stateParts.Add($"Canal {ChannelId}");
        }

        var state = stateParts.Count > 0
            ? string.Join(" • ", stateParts)
            : "Aventurándose por Sumeria";

        var partyMaximum = settings.DiscordShowParty
            ? Math.Max(0, PartyMaximum)
            : 0;
        var partyCurrent = partyMaximum > 0
            ? Math.Clamp(PartyCurrent, 0, partyMaximum)
            : 0;
        var partyId = partyMaximum > 0 && partyCurrent > 0
            ? $"nosgm-channel-{Math.Max(0, ChannelId)}"
            : null;

        return new DiscordPresenceActivity(
            details,
            state,
            SessionStartedUnixSeconds > 0
                ? SessionStartedUnixSeconds
                : fallbackStartedAtUnixSeconds,
            string.IsNullOrWhiteSpace(LargeImageKey) ? "nosgm" : LargeImageKey,
            string.IsNullOrWhiteSpace(LargeImageText) ? "NosGM" : LargeImageText,
            string.IsNullOrWhiteSpace(SmallImageKey) ? null : SmallImageKey,
            string.IsNullOrWhiteSpace(SmallImageText) ? null : SmallImageText,
            partyId,
            partyCurrent,
            partyMaximum);
    }

    public static LauncherPresenceState LauncherStage(
        string details,
        string state,
        long startedAtUnixSeconds)
    {
        return new LauncherPresenceState
        {
            Activity = "launcher",
            Details = details,
            MapName = string.Empty,
            CharacterName = string.Empty,
            SessionStartedUnixSeconds = startedAtUnixSeconds,
            LargeImageKey = "nosgm",
            LargeImageText = "NosGM Launcher",
            SmallImageKey = "launcher",
            SmallImageText = state
        };
    }

    private static string GenericActivityText(string activity)
    {
        return activity?.Trim().ToLowerInvariant() switch
        {
            "raid" => "Participando en una raid",
            "timespace" => "Explorando una Piedra del Tiempo",
            "instant_battle" => "Participando en Instant Battle",
            "lod" => "Combatiendo en Tierra de la Muerte",
            "caligor" => "Luchando contra Caligor",
            "icebreaker" => "Participando en Ice Breaker",
            "arena" => "Compitiendo en la arena",
            "talent_arena" => "Compitiendo en Talent Arena",
            "rainbow_battle" => "Participando en Rainbow Battle",
            "glacernon" => "Aventurándose por Glacernon",
            "ship" => "Viajando entre continentes",
            "celestial_spire" => "Ascendiendo la Aguja Celestial",
            "event" => "Participando en un evento",
            "launcher" => "Preparando NosGM",
            _ => "Jugando en NosGM"
        };
    }
}
