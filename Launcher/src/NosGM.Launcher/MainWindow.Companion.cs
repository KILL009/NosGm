// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NosGM.Launcher;

internal static class LauncherCompanionModule
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.InitializeCompanionMode();
        }
    }
}

public partial class MainWindow
{
    private sealed record CompanionAlertCandidate(
        string Key,
        string Title,
        string Message,
        bool Warning,
        int Priority,
        DateTimeOffset SortTime);

    private readonly DispatcherTimer _companionPollTimer = new()
    {
        Interval = TimeSpan.FromMinutes(1)
    };
    private readonly SemaphoreSlim _companionPollGate = new(1, 1);
    private readonly CancellationTokenSource _companionLifetime = new();
    private LauncherLiveOperationsClient? _companionOperationsClient;
    private LauncherCompanionAlertState _companionAlertState =
        LauncherCompanionAlertState.Empty;
    private LauncherTrayIcon? _companionTrayIcon;
    private Button? _companionButton;
    private Process? _companionGameProcess;
    private Task? _companionInitializationTask;
    private string? _companionPortalBaseUri;
    private bool _companionInitialized;
    private bool _companionHidden;
    private bool _companionExitRequested;
    private bool _companionCloseDrainStarted;
    private bool _companionShutdownDrained;
    private bool _companionClosed;

    internal void InitializeCompanionMode()
    {
        if (_companionInitialized)
        {
            return;
        }

        _companionInitialized = true;
        Closing += MainWindow_CompanionClosing;
        Closed += MainWindow_CompanionClosed;
        ModernGameLauncher.GameLaunched += CompanionGameLaunched;
        LanguageComboBox.SelectionChanged += CompanionLanguage_SelectionChanged;
        _companionPollTimer.Tick += CompanionPollTimer_Tick;
        _companionInitializationTask = InitializeCompanionModeAsync();
        _ = ObserveCompanionInitializationAsync(_companionInitializationTask);
    }

    private async Task InitializeCompanionModeAsync()
    {
        for (var attempt = 0;
             attempt < 100 && (!_languageSelectionReady || !IsLoaded);
             attempt++)
        {
            await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    _companionLifetime.Token)
                .ConfigureAwait(true);
        }

        if (_companionLifetime.IsCancellationRequested ||
            !_languageSelectionReady ||
            !IsLoaded)
        {
            return;
        }

        await AttachCompanionButtonAsync().ConfigureAwait(true);
        _companionAlertState = await LauncherCompanionAlertStateStore.LoadAsync(
                _companionLifetime.Token)
            .ConfigureAwait(true);
        if (!_companionLifetime.IsCancellationRequested && !_companionClosed)
        {
            ApplyCompanionSettings();
        }
    }

    private async Task ObserveCompanionInitializationAsync(Task initialization)
    {
        try
        {
            await initialization.ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_companionLifetime.IsCancellationRequested)
        {
            // Normal shutdown while the companion is attaching to the launcher.
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            // Companion is optional and cannot prevent the launcher from opening.
        }
    }

    private async Task AttachCompanionButtonAsync()
    {
        for (var attempt = 0; attempt < 60 && IsLoaded; attempt++)
        {
            _companionLifetime.Token.ThrowIfCancellationRequested();
            var communityButton = FindVisualChildren<Button>(this)
                .FirstOrDefault(button =>
                    button.Content is string content &&
                    (content.Contains("Comunidad", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("Community", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("Foro", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("Forum", StringComparison.OrdinalIgnoreCase)));
            if (communityButton is not null &&
                VisualTreeHelper.GetParent(communityButton) is Panel parent)
            {
                _companionButton = new Button
                {
                    Height = communityButton.Height,
                    MinWidth = 112,
                    Margin = communityButton.Margin,
                    Padding = communityButton.Padding,
                    Style = communityButton.Style,
                    ToolTip = "NosGM Companion, bandeja y alertas de eventos."
                };
                _companionButton.Click += OpenCompanionSettings_Click;
                var index = parent.Children.IndexOf(communityButton);
                parent.Children.Insert(
                    Math.Min(index + 1, parent.Children.Count),
                    _companionButton);
                RefreshCompanionButtonText();
                return;
            }

            await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    _companionLifetime.Token)
                .ConfigureAwait(true);
        }
    }

    private void ApplyCompanionSettings()
    {
        RefreshCompanionButtonText();
        if (_companionClosed)
        {
            return;
        }

        if (_settings.CompanionModeEnabled)
        {
            EnsureCompanionTrayIcon();
            _companionTrayIcon!.SetLanguage(IsCompanionSpanish());
            _companionTrayIcon.SetVisible(true);
            EnsureCompanionOperationsClient();
            _companionPollTimer.Start();
            _ = RefreshCompanionAlertsAsync();
            return;
        }

        _companionPollTimer.Stop();
        if (_companionHidden)
        {
            RestoreFromCompanionTray();
        }

        _companionTrayIcon?.SetVisible(false);
    }

    private void EnsureCompanionTrayIcon()
    {
        if (_companionTrayIcon is not null)
        {
            return;
        }

        _companionTrayIcon = new LauncherTrayIcon(this, IsCompanionSpanish());
        _companionTrayIcon.OpenRequested += CompanionTrayOpenRequested;
        _companionTrayIcon.SettingsRequested += CompanionTraySettingsRequested;
        _companionTrayIcon.ExitRequested += CompanionTrayExitRequested;
    }

    private void EnsureCompanionOperationsClient()
    {
        if (_companionOperationsClient is not null &&
            string.Equals(
                _companionPortalBaseUri,
                _settings.PortalBaseUri,
                StringComparison.Ordinal))
        {
            return;
        }

        if (_companionPollGate.CurrentCount == 0)
        {
            throw new InvalidOperationException(
                "Companion operations cannot change while a public request is active.");
        }

        _companionOperationsClient?.Dispose();
        _companionOperationsClient = new LauncherLiveOperationsClient(
            _settings.PortalBaseUri);
        _companionPortalBaseUri = _settings.PortalBaseUri;
    }

    private void CompanionGameLaunched(Process process, string accountName)
    {
        _ = accountName;
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => TrackCompanionGameProcess(process));
    }

    private void TrackCompanionGameProcess(Process process)
    {
        if (_companionClosed)
        {
            return;
        }

        DetachCompanionGameProcess();
        _companionGameProcess = process;
        try
        {
            process.EnableRaisingEvents = true;
            process.Exited += CompanionGameProcess_Exited;
            if (process.HasExited)
            {
                CompanionGameProcess_Exited(process, EventArgs.Empty);
                return;
            }
        }
        catch (InvalidOperationException)
        {
            _companionGameProcess = null;
            return;
        }

        if (_settings.CompanionModeEnabled)
        {
            EnsureCompanionTrayIcon();
            _companionTrayIcon!.SetVisible(true);
            HideToCompanionTray();
            _companionTrayIcon.ShowNotification(
                "NosGM Companion",
                IsCompanionSpanish()
                    ? "NosGM seguirá activo en la bandeja mientras juegas."
                    : "NosGM will remain active in the tray while you play.");
        }
    }

    private void CompanionGameProcess_Exited(object? sender, EventArgs e)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (_companionClosed)
            {
                return;
            }

            DetachCompanionGameProcess();
            if (_settings.CompanionModeEnabled)
            {
                EnsureCompanionTrayIcon();
                _companionTrayIcon!.ShowNotification(
                    "NosGM Companion",
                    IsCompanionSpanish()
                        ? "El cliente se cerró. El launcher está listo nuevamente."
                        : "The game client closed. The launcher is ready again.");
            }

            if (_settings.CompanionRestoreAfterGame && _companionHidden)
            {
                RestoreFromCompanionTray();
            }
        });
    }

    private void DetachCompanionGameProcess()
    {
        var process = _companionGameProcess;
        _companionGameProcess = null;
        if (process is null)
        {
            return;
        }

        try
        {
            process.Exited -= CompanionGameProcess_Exited;
            process.Dispose();
        }
        catch
        {
            // The process may already be unavailable during Windows shutdown.
        }
    }

    private bool IsCompanionGameRunning()
    {
        try
        {
            return _companionGameProcess is { HasExited: false };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void MainWindow_CompanionClosing(object? sender, CancelEventArgs e)
    {
        if (_companionShutdownDrained)
        {
            return;
        }

        if (!_companionExitRequested &&
            _settings.CompanionModeEnabled &&
            IsCompanionGameRunning())
        {
            e.Cancel = true;
            HideToCompanionTray();
            return;
        }

        var initializationActive =
            _companionInitializationTask is { IsCompleted: false };
        var pollActive = _companionPollGate.CurrentCount == 0;
        if (_companionCloseDrainStarted ||
            (!initializationActive && !pollActive))
        {
            return;
        }

        e.Cancel = true;
        _companionCloseDrainStarted = true;
        _companionExitRequested = true;
        _companionPollTimer.Stop();
        _companionLifetime.Cancel();
        _ = DrainCompanionAndCloseAsync();
    }

    private async Task DrainCompanionAndCloseAsync()
    {
        try
        {
            if (_companionInitializationTask is { } initialization)
            {
                try
                {
                    await initialization.ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // The initialization observer already treats shutdown as normal.
                }
            }

            var entered = false;
            try
            {
                entered = await _companionPollGate.WaitAsync(
                        TimeSpan.FromSeconds(4))
                    .ConfigureAwait(true);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            finally
            {
                if (entered)
                {
                    _companionPollGate.Release();
                }
            }
        }
        finally
        {
            if (!_companionClosed &&
                !Dispatcher.HasShutdownStarted &&
                !Dispatcher.HasShutdownFinished)
            {
                _companionShutdownDrained = true;
                Close();
            }
        }
    }

    private void HideToCompanionTray()
    {
        if (_companionHidden)
        {
            return;
        }

        EnsureCompanionTrayIcon();
        _companionTrayIcon!.SetVisible(true);
        _companionHidden = true;
        ShowInTaskbar = false;
        Hide();
    }

    private void RestoreFromCompanionTray()
    {
        if (_companionClosed)
        {
            return;
        }

        _companionHidden = false;
        ShowInTaskbar = true;
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private async void OpenCompanionSettings_Click(object sender, RoutedEventArgs e)
        => await OpenCompanionSettingsAsync();

    private async Task OpenCompanionSettingsAsync()
    {
        if (!_languageSelectionReady || _companionClosed)
        {
            return;
        }

        RestoreFromCompanionTray();
        var updated = LauncherCompanionSettingsWindow.Show(
            this,
            _settings,
            ShowCompanionTestNotification);
        if (updated is null)
        {
            return;
        }

        try
        {
            await LauncherSettingsStore.SaveAsync(updated).ConfigureAwait(true);
            _settings = updated;
            ApplyCompanionSettings();
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void ShowCompanionTestNotification()
    {
        EnsureCompanionTrayIcon();
        _companionTrayIcon!.SetVisible(true);
        _companionTrayIcon.ShowNotification(
            IsCompanionSpanish() ? "Prueba de NosGM" : "NosGM test",
            IsCompanionSpanish()
                ? "Las alertas del Companion funcionan correctamente."
                : "Companion alerts are working correctly.");

        if (!_settings.CompanionModeEnabled)
        {
            _ = HideTemporaryTestIconAsync();
        }
    }

    private async Task HideTemporaryTestIconAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(12), _companionLifetime.Token)
                .ConfigureAwait(true);
            if (!_settings.CompanionModeEnabled && !_companionHidden)
            {
                _companionTrayIcon?.SetVisible(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal launcher shutdown.
        }
    }

    private async void CompanionPollTimer_Tick(object? sender, EventArgs e)
        => await RefreshCompanionAlertsAsync();

    private async Task RefreshCompanionAlertsAsync()
    {
        if (_companionClosed ||
            _companionLifetime.IsCancellationRequested ||
            !_settings.CompanionModeEnabled ||
            (!_settings.EventAlertsEnabled && !_settings.MaintenanceAlertsEnabled) ||
            !await _companionPollGate.WaitAsync(0).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            if (_companionAlertState.MutedUntil is { } mutedUntil &&
                mutedUntil > DateTimeOffset.UtcNow)
            {
                return;
            }

            EnsureCompanionOperationsClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                _companionLifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var dashboard = await _companionOperationsClient!.GetDashboardAsync(
                    timeout.Token)
                .ConfigureAwait(true);
            if (_companionClosed || _companionLifetime.IsCancellationRequested)
            {
                return;
            }

            var candidate = SelectCompanionAlert(dashboard.Operations);
            if (candidate is null ||
                LauncherCompanionAlertStateStore.WasDelivered(
                    _companionAlertState,
                    candidate.Key))
            {
                return;
            }

            EnsureCompanionTrayIcon();
            _companionTrayIcon!.SetVisible(true);
            _companionTrayIcon.ShowNotification(
                candidate.Title,
                candidate.Message,
                candidate.Warning);
            _companionAlertState = LauncherCompanionAlertStateStore.Remember(
                _companionAlertState,
                candidate.Key);
            try
            {
                await LauncherCompanionAlertStateStore.SaveAsync(
                        _companionAlertState,
                        CancellationToken.None)
                    .ConfigureAwait(true);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                // Notification delivery does not depend on optional local history.
            }
        }
        catch (OperationCanceledException) when (
            _companionLifetime.IsCancellationRequested || !_companionClosed)
        {
            // Shutdown or the next timer cycle will resolve the operation.
        }
        catch (Exception exception) when (
            exception is IOException or HttpRequestException or InvalidDataException or
                JsonException or ObjectDisposedException or InvalidOperationException)
        {
            // Public event alerts fail closed and never interrupt game launch.
        }
        finally
        {
            _companionPollGate.Release();
        }
    }

    private CompanionAlertCandidate? SelectCompanionAlert(
        LauncherOperationsSnapshot operations)
    {
        var now = DateTimeOffset.UtcNow;
        var reminder = TimeSpan.FromMinutes(_settings.EventReminderMinutes);
        var candidates = new List<CompanionAlertCandidate>();

        if (_settings.MaintenanceAlertsEnabled)
        {
            var maintenance = operations.Maintenance;
            if (maintenance.IsActive)
            {
                var key = $"maintenance:active:{maintenance.StartsAt?.UtcDateTime.Ticks ?? 0}";
                candidates.Add(new CompanionAlertCandidate(
                    key,
                    IsCompanionSpanish()
                        ? "Mantenimiento en curso"
                        : "Maintenance in progress",
                    BuildCompanionMaintenanceMessage(maintenance, active: true),
                    Warning: true,
                    Priority: 100,
                    maintenance.StartsAt ?? now));
            }
            else if (maintenance.StartsAt is { } maintenanceStart &&
                     maintenanceStart > now &&
                     maintenanceStart - now <= reminder)
            {
                var key = $"maintenance:reminder:{maintenanceStart.UtcDateTime.Ticks}";
                candidates.Add(new CompanionAlertCandidate(
                    key,
                    IsCompanionSpanish()
                        ? "Mantenimiento próximo"
                        : "Upcoming maintenance",
                    BuildCompanionMaintenanceMessage(maintenance, active: false),
                    Warning: true,
                    Priority: 90,
                    maintenanceStart));
            }
        }

        if (_settings.EventAlertsEnabled)
        {
            foreach (var item in operations.Events)
            {
                if (item.StartsAt <= now &&
                    item.EndsAt > now &&
                    now - item.StartsAt <= TimeSpan.FromMinutes(2))
                {
                    candidates.Add(new CompanionAlertCandidate(
                        $"event:{item.Id}:{item.StartsAt.UtcDateTime.Ticks}:started",
                        item.Title,
                        BuildCompanionEventMessage(item, active: true, now),
                        Warning: false,
                        Priority: 80,
                        item.StartsAt));
                }
                else if (item.StartsAt > now &&
                         item.StartsAt - now <= reminder)
                {
                    candidates.Add(new CompanionAlertCandidate(
                        $"event:{item.Id}:{item.StartsAt.UtcDateTime.Ticks}:reminder",
                        item.Title,
                        BuildCompanionEventMessage(item, active: false, now),
                        Warning: false,
                        Priority: 70,
                        item.StartsAt));
                }
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.SortTime)
            .FirstOrDefault(candidate =>
                !LauncherCompanionAlertStateStore.WasDelivered(
                    _companionAlertState,
                    candidate.Key));
    }

    private string BuildCompanionMaintenanceMessage(
        LauncherMaintenanceStatus maintenance,
        bool active)
    {
        var title = string.IsNullOrWhiteSpace(maintenance.Title)
            ? IsCompanionSpanish()
                ? "Mantenimiento del servidor"
                : "Server maintenance"
            : maintenance.Title;
        if (active)
        {
            return maintenance.EndsAt is { } end
                ? $"{title} • {(IsCompanionSpanish() ? "termina" : "ends")} {end.ToLocalTime():HH:mm}"
                : title;
        }

        return maintenance.StartsAt is { } start
            ? $"{title} • {(IsCompanionSpanish() ? "comienza" : "starts")} {start.ToLocalTime():HH:mm}"
            : title;
    }

    private string BuildCompanionEventMessage(
        LauncherCalendarEvent item,
        bool active,
        DateTimeOffset now)
    {
        var timing = active
            ? IsCompanionSpanish()
                ? "Ya está en curso"
                : "Now in progress"
            : IsCompanionSpanish()
                ? $"Comienza en {Math.Max(1, (int)Math.Ceiling((item.StartsAt - now).TotalMinutes))} min"
                : $"Starts in {Math.Max(1, (int)Math.Ceiling((item.StartsAt - now).TotalMinutes))} min";
        var channel = item.Channel == 0
            ? IsCompanionSpanish()
                ? "Todos los canales"
                : "All channels"
            : $"{(IsCompanionSpanish() ? "Canal" : "Channel")} {item.Channel}";
        var levels = item.MinimumLevel == 0 && item.MaximumLevel == 0
            ? IsCompanionSpanish()
                ? "Todos los niveles"
                : "All levels"
            : $"{(IsCompanionSpanish() ? "Niveles" : "Levels")} {item.MinimumLevel}-{item.MaximumLevel}";
        return $"{timing} • {channel} • {levels}";
    }

    private void CompanionTrayOpenRequested(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(RestoreFromCompanionTray);

    private void CompanionTraySettingsRequested(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(async () => await OpenCompanionSettingsAsync());

    private void CompanionTrayExitRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _companionExitRequested = true;
            Close();
        });
    }

    private void CompanionLanguage_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        RefreshCompanionButtonText();
        _companionTrayIcon?.SetLanguage(IsCompanionSpanish());
    }

    private void RefreshCompanionButtonText()
    {
        if (_companionButton is null)
        {
            return;
        }

        _companionButton.Content = IsCompanionSpanish()
            ? "🔔 Alertas"
            : "🔔 Alerts";
    }

    private bool IsCompanionSpanish()
        => string.Equals(
            _settings.Language,
            "es",
            StringComparison.OrdinalIgnoreCase);

    private void MainWindow_CompanionClosed(object? sender, EventArgs e)
    {
        if (_companionClosed)
        {
            return;
        }

        _companionClosed = true;
        Closing -= MainWindow_CompanionClosing;
        Closed -= MainWindow_CompanionClosed;
        ModernGameLauncher.GameLaunched -= CompanionGameLaunched;
        LanguageComboBox.SelectionChanged -= CompanionLanguage_SelectionChanged;
        _companionPollTimer.Stop();
        _companionPollTimer.Tick -= CompanionPollTimer_Tick;
        _companionLifetime.Cancel();
        DetachCompanionGameProcess();
        _companionOperationsClient?.Dispose();
        _companionOperationsClient = null;
        if (_companionButton is not null)
        {
            _companionButton.Click -= OpenCompanionSettings_Click;
        }

        if (_companionTrayIcon is not null)
        {
            _companionTrayIcon.OpenRequested -= CompanionTrayOpenRequested;
            _companionTrayIcon.SettingsRequested -= CompanionTraySettingsRequested;
            _companionTrayIcon.ExitRequested -= CompanionTrayExitRequested;
            _companionTrayIcon.Dispose();
            _companionTrayIcon = null;
        }

        _companionLifetime.Dispose();
        _companionPollGate.Dispose();
    }
}
