// SPDX-License-Identifier: MIT

using System.Text.Json;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed record LauncherCompanionAlertState(
    DateTimeOffset UpdatedAt,
    string[] DeliveredKeys,
    DateTimeOffset? MutedUntil)
{
    public static LauncherCompanionAlertState Empty { get; } = new(
        DateTimeOffset.UtcNow,
        Array.Empty<string>(),
        null);
}

internal static class LauncherCompanionAlertStateStore
{
    private const int MaximumStateBytes = 64 * 1024;
    private const int MaximumDeliveredKeys = 200;
    private static readonly TimeSpan MaximumStateAge = TimeSpan.FromDays(14);

    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "event-alert-state.json");

    public static async Task<LauncherCompanionAlertState> LoadAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(StatePath))
        {
            return LauncherCompanionAlertState.Empty;
        }

        try
        {
            var info = new FileInfo(StatePath);
            if (info.Length <= 0 || info.Length > MaximumStateBytes)
            {
                return LauncherCompanionAlertState.Empty;
            }

            var bytes = await File.ReadAllBytesAsync(StatePath, cancellationToken)
                .ConfigureAwait(false);
            var state = JsonSerializer.Deserialize<LauncherCompanionAlertState>(bytes);
            if (!IsValid(state) || DateTimeOffset.UtcNow - state!.UpdatedAt > MaximumStateAge)
            {
                return LauncherCompanionAlertState.Empty;
            }

            return state;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return LauncherCompanionAlertState.Empty;
        }
    }

    public static LauncherCompanionAlertState Remember(
        LauncherCompanionAlertState state,
        string key)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!IsSafeKey(key))
        {
            throw new InvalidDataException("Companion alert key is invalid.");
        }

        var keys = new[] { key }
            .Concat(state.DeliveredKeys ?? Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .Take(MaximumDeliveredKeys)
            .ToArray();
        return state with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            DeliveredKeys = keys
        };
    }

    public static LauncherCompanionAlertState MuteFor(
        LauncherCompanionAlertState state,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        return state with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            MutedUntil = DateTimeOffset.UtcNow.Add(duration)
        };
    }

    public static LauncherCompanionAlertState ClearMute(LauncherCompanionAlertState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            MutedUntil = null
        };
    }

    public static bool WasDelivered(LauncherCompanionAlertState state, string key)
        => state.DeliveredKeys?.Contains(key, StringComparer.Ordinal) == true;

    public static async Task SaveAsync(
        LauncherCompanionAlertState state,
        CancellationToken cancellationToken)
    {
        if (!IsValid(state))
        {
            throw new InvalidDataException("Companion alert state is invalid.");
        }

        await JsonSupport.WriteAtomicAsync(StatePath, state, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsValid(LauncherCompanionAlertState? state)
    {
        if (state is null ||
            state.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(5) ||
            state.DeliveredKeys is null ||
            state.DeliveredKeys.Length > MaximumDeliveredKeys ||
            state.DeliveredKeys.Distinct(StringComparer.Ordinal).Count() !=
            state.DeliveredKeys.Length ||
            state.DeliveredKeys.Any(key => !IsSafeKey(key)) ||
            state.MutedUntil > DateTimeOffset.UtcNow.AddDays(1))
        {
            return false;
        }

        return true;
    }

    private static bool IsSafeKey(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length <= 180 &&
           value.All(character =>
               char.IsLetterOrDigit(character) ||
               character is '-' or '_' or '.' or ':');
}
