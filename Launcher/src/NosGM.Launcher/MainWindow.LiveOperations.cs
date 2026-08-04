// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NosGM.Launcher;

internal static class LauncherLiveOperationsModule
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
            window.StartLiveOperationsDashboard();
        }
    }
}

public partial class MainWindow
{
    private readonly DispatcherTimer _operationsRefreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(20)
    };
    private readonly DispatcherTimer _operationsCountdownTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };
    private readonly SemaphoreSlim _operationsGate = new(1, 1);

    private LauncherLiveOperationsClient? _operationsClient;
    private CancellationTokenSource? _operationsCancellation;
    private LauncherOperationsDashboard? _operationsDashboard;
    private TextBlock? _operationsRatesTextBlock;
    private TextBlock? _operationsChannelsTextBlock;
    private TextBlock? _operationsEventTextBlock;
    private bool _operationsInitialized;
    private bool _operationsClosed;

    internal async void StartLiveOperationsDashboard()
    {
        if (_operationsInitialized)
        {
            return;
        }

        _operationsInitialized = true;
        for (var attempt = 0; attempt < 100 && !_languageSelectionReady && IsLoaded; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        if (!IsLoaded || !_languageSelectionReady || _operationsClosed)
        {
            return;
        }

        InjectOperationsSummaryControls();
        _operationsClient = new LauncherLiveOperationsClient(_settings.PortalBaseUri);
        _operationsRefreshTimer.Tick += OperationsRefreshTimer_Tick;
        _operationsCountdownTimer.Tick += OperationsCountdownTimer_Tick;
        Closed += MainWindow_OperationsClosed;
        _operationsRefreshTimer.Start();
        _operationsCountdownTimer.Start();
        await RefreshOperationsAsync();
    }

    private void InjectOperationsSummaryControls()
    {
        if (StatusTextBlock.Parent is not StackPanel host)
        {
            return;
        }

        _operationsRatesTextBlock = CreateOperationsLine("Tasas: conectando con World...");
        _operationsChannelsTextBlock = CreateOperationsLine("Canales: consultando población...");
        _operationsEventTextBlock = CreateOperationsLine("Calendario: sincronizando...");
        _operationsEventTextBlock.FontWeight = FontWeights.SemiBold;

        host.Children.Add(_operationsRatesTextBlock);
        host.Children.Add(_operationsChannelsTextBlock);
        host.Children.Add(_operationsEventTextBlock);
    }

    private static TextBlock CreateOperationsLine(string text)
    {
        return new TextBlock
        {
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 10,
            Foreground = FrozenBrush(113, 128, 156),
            Text = text,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = text
        };
    }

    private async void OperationsRefreshTimer_Tick(object? sender, EventArgs e)
        => await RefreshOperationsAsync();

    private void OperationsCountdownTimer_Tick(object? sender, EventArgs e)
        => UpdateOperationsCountdown();

    private async Task RefreshOperationsAsync()
    {
        if (_operationsClosed || _operationsClient is null)
        {
            return;
        }

        if (!await _operationsGate.WaitAsync(0))
        {
            return;
        }

        _operationsCancellation?.Cancel();
        _operationsCancellation?.Dispose();
        _operationsCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            var dashboard = await _operationsClient.GetDashboardAsync(
                _operationsCancellation.Token);
            if (_operationsClosed)
            {
                return;
            }

            _operationsDashboard = dashboard;
            ApplyOperationsRates(dashboard.Operations);
            ApplyChannelPopulation(dashboard.Status);
            UpdateOperationsCountdown();
        }
        catch (OperationCanceledException) when (!_operationsClosed)
        {
            MarkOperationsUnavailable("Operaciones: portal sin respuesta");
        }
        catch (Exception exception) when (
            !_operationsClosed
            && exception is HttpRequestException or IOException or InvalidDataException or JsonException)
        {
            MarkOperationsUnavailable("Operaciones: datos no disponibles");
        }
        finally
        {
            _operationsGate.Release();
        }
    }

    private void ApplyOperationsRates(LauncherOperationsSnapshot operations)
    {
        if (_operationsRatesTextBlock is null)
        {
            return;
        }

        var selectedIds = new[] { "xp", "hero-xp", "drop", "fairy-xp" };
        var rates = selectedIds
            .Select(id => operations.Rates.FirstOrDefault(rate =>
                string.Equals(rate.Id, id, StringComparison.OrdinalIgnoreCase)))
            .Where(rate => rate is not null)
            .Select(rate => $"{ShortRateName(rate!.Id)} ×{rate.Multiplier}")
            .ToArray();

        _operationsRatesTextBlock.Text = rates.Length == 0
            ? "Tasas: no publicadas"
            : "Tasas: " + string.Join(" • ", rates);
        _operationsRatesTextBlock.ToolTip = string.Join(
            Environment.NewLine,
            operations.Rates.Select(rate => $"{rate.Name}: ×{rate.Multiplier}"));
    }

    private void ApplyChannelPopulation(LauncherServerStatus status)
    {
        if (_operationsChannelsTextBlock is null)
        {
            return;
        }

        var channels = status.Services
            .Where(service => service.Id.StartsWith("channel-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(service => ParseChannelNumber(service.Id))
            .ToArray();
        if (channels.Length == 0)
        {
            _operationsChannelsTextBlock.Text = "Canales: sin información";
            _operationsChannelsTextBlock.ToolTip = "El snapshot público no contiene canales.";
            return;
        }

        _operationsChannelsTextBlock.Text = "Canales: " + string.Join(
            " • ",
            channels.Select(service =>
                service.Health == LauncherServiceHealth.Online
                    ? $"C{ParseChannelNumber(service.Id)} {service.OnlinePlayers:N0}"
                    : $"C{ParseChannelNumber(service.Id)} fuera"));
        _operationsChannelsTextBlock.ToolTip = string.Join(
            Environment.NewLine,
            channels.Select(service =>
                $"{service.Name}: {service.OnlinePlayers:N0} jugadores, {HealthText(service.Health)}"));
    }

    private void UpdateOperationsCountdown()
    {
        if (_operationsEventTextBlock is null || _operationsDashboard is null)
        {
            return;
        }

        var operations = _operationsDashboard.Operations;
        var now = DateTimeOffset.UtcNow;
        var maintenance = operations.Maintenance;
        if (maintenance.IsActive)
        {
            _operationsEventTextBlock.Foreground = FrozenBrush(255, 184, 77);
            _operationsEventTextBlock.Text = maintenance.EndsAt is { } maintenanceEnd
                && maintenanceEnd > now
                    ? $"⚠ {SafeLabel(maintenance.Title, "Mantenimiento")} • termina en {FormatRemaining(maintenanceEnd - now)}"
                    : $"⚠ {SafeLabel(maintenance.Title, "Mantenimiento en curso")}";
            _operationsEventTextBlock.ToolTip = SafeLabel(
                maintenance.Message,
                "El servidor está en mantenimiento.");
            return;
        }

        if (maintenance.StartsAt is { } maintenanceStart
            && maintenanceStart > now
            && maintenanceStart - now <= TimeSpan.FromHours(24))
        {
            _operationsEventTextBlock.Foreground = FrozenBrush(255, 184, 77);
            _operationsEventTextBlock.Text =
                $"Mantenimiento en {FormatRemaining(maintenanceStart - now)}";
            _operationsEventTextBlock.ToolTip = SafeLabel(
                maintenance.Message,
                maintenance.Title);
            return;
        }

        var active = operations.Events
            .Where(item => item.StartsAt <= now && item.EndsAt > now)
            .OrderBy(item => item.EndsAt)
            .FirstOrDefault();
        if (active is not null)
        {
            _operationsEventTextBlock.Foreground = FrozenBrush(62, 232, 143);
            _operationsEventTextBlock.Text =
                $"● En curso: {active.Title} • {FormatRemaining(active.EndsAt - now)} restantes";
            _operationsEventTextBlock.ToolTip = EventToolTip(active);
            return;
        }

        var next = operations.Events
            .Where(item => item.StartsAt > now)
            .OrderBy(item => item.StartsAt)
            .FirstOrDefault();
        if (next is null)
        {
            _operationsEventTextBlock.Foreground = FrozenBrush(113, 128, 156);
            _operationsEventTextBlock.Text = operations.IsStale
                ? "Calendario: datos antiguos"
                : "Calendario: sin eventos programados";
            _operationsEventTextBlock.ToolTip =
                "Los eventos aparecerán cuando se publiquen en el calendario firmado.";
            return;
        }

        _operationsEventTextBlock.Foreground = FrozenBrush(192, 132, 252);
        _operationsEventTextBlock.Text =
            $"Próximo: {next.Title} • en {FormatRemaining(next.StartsAt - now)}";
        _operationsEventTextBlock.ToolTip = EventToolTip(next)
                                             + (operations.IsStale
                                                 ? Environment.NewLine + "Advertencia: datos antiguos."
                                                 : string.Empty);
    }

    private void MarkOperationsUnavailable(string text)
    {
        if (_operationsDashboard is not null)
        {
            return;
        }

        if (_operationsRatesTextBlock is not null)
        {
            _operationsRatesTextBlock.Text = "Tasas: no disponibles";
        }

        if (_operationsChannelsTextBlock is not null)
        {
            _operationsChannelsTextBlock.Text = "Canales: usa la comprobación local";
        }

        if (_operationsEventTextBlock is not null)
        {
            _operationsEventTextBlock.Foreground = FrozenBrush(255, 184, 77);
            _operationsEventTextBlock.Text = text;
            _operationsEventTextBlock.ToolTip =
                "El juego puede continuar. El dashboard volverá a intentarlo automáticamente.";
        }
    }

    private static string EventToolTip(LauncherCalendarEvent item)
    {
        var details = new List<string>
        {
            item.Details,
            $"Inicio: {item.StartsAt.ToLocalTime():dd/MM/yyyy HH:mm}",
            $"Fin: {item.EndsAt.ToLocalTime():dd/MM/yyyy HH:mm}"
        };
        if (item.Channel > 0)
        {
            details.Add($"Canal: {item.Channel}");
        }

        if (item.MaximumLevel > 0)
        {
            details.Add($"Nivel: {item.MinimumLevel}-{item.MaximumLevel}");
        }

        return string.Join(
            Environment.NewLine,
            details.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ShortRateName(string id)
        => id.ToLowerInvariant() switch
        {
            "xp" => "EXP",
            "hero-xp" => "Hero",
            "drop" => "Drop",
            "fairy-xp" => "Fairy",
            _ => id
        };

    private static int ParseChannelNumber(string id)
    {
        var separator = id.LastIndexOf('-');
        return separator >= 0
               && int.TryParse(id[(separator + 1)..], out var channel)
            ? channel
            : int.MaxValue;
    }

    private static string HealthText(LauncherServiceHealth health)
        => health switch
        {
            LauncherServiceHealth.Online => "en línea",
            LauncherServiceHealth.Degraded => "degradado",
            _ => "fuera de línea"
        };

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "ahora";
        }

        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays}d {remaining.Hours:00}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes:00}m";
        }

        return $"{Math.Max(0, remaining.Minutes):00}m {Math.Max(0, remaining.Seconds):00}s";
    }

    private static string SafeLabel(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private void MainWindow_OperationsClosed(object? sender, EventArgs e)
    {
        if (_operationsClosed)
        {
            return;
        }

        _operationsClosed = true;
        Closed -= MainWindow_OperationsClosed;
        _operationsRefreshTimer.Stop();
        _operationsCountdownTimer.Stop();
        _operationsRefreshTimer.Tick -= OperationsRefreshTimer_Tick;
        _operationsCountdownTimer.Tick -= OperationsCountdownTimer_Tick;
        _operationsCancellation?.Cancel();
        _operationsCancellation?.Dispose();
        _operationsCancellation = null;
        _operationsClient?.Dispose();
        _operationsClient = null;
        _operationsGate.Dispose();
    }
}
