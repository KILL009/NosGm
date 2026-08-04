// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NosGM.Launcher;

public partial class MainWindow
{
    private readonly DispatcherTimer _liveContentTimer = new()
    {
        Interval = TimeSpan.FromSeconds(30)
    };
    private readonly SemaphoreSlim _liveContentGate = new(1, 1);
    private LauncherLiveContentClient? _liveContentClient;
    private CancellationTokenSource? _liveContentCancellation;
    private TextBlock?[] _liveNewsTitles = [];
    private TextBlock?[] _liveNewsDates = [];
    private TextBlock? _liveNewsLink;
    private Button? _liveRefreshButton;
    private bool _liveContentInitialized;
    private bool _liveContentClosed;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Loaded += MainWindow_LiveContentLoaded;
        Closed += MainWindow_LiveContentClosed;
    }

    private async void MainWindow_LiveContentLoaded(object sender, RoutedEventArgs e)
    {
        if (_liveContentInitialized)
        {
            return;
        }

        _liveContentInitialized = true;

        // MainWindow_Loaded reads settings and performs transactional recovery.
        // Wait for that initialization instead of racing it with the live client.
        for (var attempt = 0; attempt < 100 && !_languageSelectionReady && IsLoaded; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        if (!IsLoaded || !_languageSelectionReady || _liveContentClosed)
        {
            return;
        }

        ResolveLiveDashboardControls();
        _liveContentClient = new LauncherLiveContentClient(_settings.PortalBaseUri);
        _liveContentTimer.Tick += LiveContentTimer_Tick;
        _liveContentTimer.Start();

        // Replace the legacy local-only timer with one dashboard cycle. Local TCP
        // probes remain the fallback, then signed portal data enriches the result.
        _serverStatusTimer.Tick -= ServerStatusTimer_Tick;
        _serverStatusTimer.Tick += DashboardStatusTimer_Tick;

        if (_liveRefreshButton is not null)
        {
            _liveRefreshButton.Click -= RefreshServerStatus_Click;
            _liveRefreshButton.Click += RefreshDashboard_Click;
        }

        if (_liveNewsLink is not null)
        {
            _liveNewsLink.MouseLeftButtonUp -= OpenNews_Click;
            _liveNewsLink.MouseLeftButtonUp += OpenLiveNews_Click;
        }

        LanguageComboBox.SelectionChanged += LiveLanguage_SelectionChanged;

        var cached = await LauncherLiveContentCache.LoadAsync(CancellationToken.None);
        if (cached is not null && !_liveContentClosed)
        {
            ApplyLiveContent(cached, fromCache: true);
        }

        await RefreshLiveContentAsync();
    }

    private async void DashboardStatusTimer_Tick(object? sender, EventArgs e)
        => await RefreshDashboardAsync();

    private async void LiveContentTimer_Tick(object? sender, EventArgs e)
        => await RefreshLiveContentAsync();

    private async void RefreshDashboard_Click(object sender, RoutedEventArgs e)
        => await RefreshDashboardAsync();

    private async Task RefreshDashboardAsync()
    {
        await RefreshServerStatusAsync();
        await RefreshLiveContentAsync();
    }

    private async void LiveLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_liveContentInitialized && _languageSelectionReady)
        {
            await RefreshLiveContentAsync();
        }
    }

    private async Task RefreshLiveContentAsync()
    {
        if (_liveContentClosed || _liveContentClient is null)
        {
            return;
        }

        if (!await _liveContentGate.WaitAsync(0))
        {
            return;
        }

        _liveContentCancellation?.Cancel();
        _liveContentCancellation?.Dispose();
        _liveContentCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            var snapshot = await _liveContentClient.GetSnapshotAsync(
                _settings.Language,
                _liveContentCancellation.Token);
            if (_liveContentClosed)
            {
                return;
            }

            ApplyLiveContent(snapshot, fromCache: false);

            try
            {
                await LauncherLiveContentCache.SaveAsync(snapshot, CancellationToken.None);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                // A cache failure never hides valid live data from the player.
            }
        }
        catch (OperationCanceledException) when (!_liveContentClosed)
        {
            MarkLiveContentUnavailable("Portal sin respuesta");
        }
        catch (Exception exception) when (
            !_liveContentClosed &&
            exception is HttpRequestException or IOException or InvalidDataException or JsonException)
        {
            MarkLiveContentUnavailable("Portal no disponible");
        }
        finally
        {
            _liveContentGate.Release();
        }
    }

    private void ApplyLiveContent(
        LauncherLiveContentSnapshot snapshot,
        bool fromCache)
    {
        ApplyNews(snapshot.News);
        ApplyRemoteService(snapshot.Status, "login", LoginStatusDot, LoginStatusText);
        ApplyRemoteService(snapshot.Status, "world", WorldStatusDot, WorldStatusText);

        var freshness = fromCache || snapshot.Status.IsStale
            ? "datos en caché"
            : "datos en vivo";
        ServerProbeTimeTextBlock.Text =
            $"{snapshot.Status.OnlinePlayers:N0} jugadores • {freshness} • {DateTime.Now:HH:mm:ss}";
        ServerProbeTimeTextBlock.ToolTip =
            $"Observado por el portal: {snapshot.Status.ObservedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}";
    }

    private void ApplyNews(IReadOnlyList<LauncherNewsItem> news)
    {
        for (var index = 0; index < _liveNewsTitles.Length; index++)
        {
            var title = _liveNewsTitles[index];
            var date = _liveNewsDates[index];
            if (title is null || date is null)
            {
                continue;
            }

            var row = title.Parent as FrameworkElement;
            if (index >= news.Count)
            {
                if (index == 0)
                {
                    row?.SetCurrentValue(VisibilityProperty, Visibility.Visible);
                    title.Text = "No hay noticias publicadas";
                    title.ToolTip = "El portal está conectado, pero no hay noticias para este idioma.";
                    date.Text = "—";
                }
                else
                {
                    row?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
                }

                continue;
            }

            var item = news[index];
            row?.SetCurrentValue(VisibilityProperty, Visibility.Visible);
            title.Text = item.Title;
            title.ToolTip = item.Summary;
            date.Text = DateTimeOffset.UtcNow - item.PublishedAt <= TimeSpan.FromDays(3)
                ? "NUEVO"
                : item.PublishedAt.ToLocalTime().ToString("dd/MM");
        }
    }

    private static void ApplyRemoteService(
        LauncherServerStatus status,
        string serviceId,
        Ellipse dot,
        TextBlock label)
    {
        var service = status.Services.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, serviceId, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            return;
        }

        switch (service.Health)
        {
            case LauncherServiceHealth.Online:
                dot.Fill = FrozenBrush(62, 232, 143);
                label.Text = "En línea";
                break;
            case LauncherServiceHealth.Degraded:
                dot.Fill = FrozenBrush(255, 184, 77);
                label.Text = "Degradado";
                break;
            default:
                dot.Fill = FrozenBrush(255, 93, 122);
                label.Text = "Fuera de línea";
                break;
        }

        label.Foreground = dot.Fill;
    }

    private void MarkLiveContentUnavailable(string message)
    {
        if (ServerProbeTimeTextBlock.Text.Contains(message, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (ServerProbeTimeTextBlock.Text.Contains("jugadores", StringComparison.OrdinalIgnoreCase))
        {
            ServerProbeTimeTextBlock.Text += $" • {message}";
        }
        else
        {
            ServerProbeTimeTextBlock.Text =
                $"{message} • comprobación local {DateTime.Now:HH:mm:ss}";
        }
    }

    private void ResolveLiveDashboardControls()
    {
        _liveNewsTitles =
        [
            FindTextBlock("Liga de Campeones NosGM"),
            FindTextBlock("Sistema de mascotas mejorado"),
            FindTextBlock("Estabilización x64 completada")
        ];
        _liveNewsDates =
        [
            FindTextBlock("NUEVO"),
            FindTextBlock("02/08"),
            FindTextBlock("01/08")
        ];
        _liveNewsLink = FindTextBlock("Ver todas  ›");
        _liveRefreshButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "↻", StringComparison.Ordinal));
    }

    private TextBlock? FindTextBlock(string text)
        => FindVisualChildren<TextBlock>(this)
            .FirstOrDefault(block => string.Equals(block.Text, text, StringComparison.Ordinal));

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var children = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < children; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void OpenLiveNews_Click(object sender, MouseButtonEventArgs e)
    {
        var portalLanguage = _settings.Language switch
        {
            "cz" => "cs",
            "jp" => "ja",
            "cn" => "zh-CN",
            _ => _settings.Language
        };
        var newsUri = new Uri(
            new Uri(_settings.PortalBaseUri, UriKind.Absolute),
            $"news?lang={Uri.EscapeDataString(portalLanguage)}");
        OpenUrl(newsUri.AbsoluteUri);
    }

    private void MainWindow_LiveContentClosed(object? sender, EventArgs e)
    {
        if (_liveContentClosed)
        {
            return;
        }

        _liveContentClosed = true;
        Loaded -= MainWindow_LiveContentLoaded;
        Closed -= MainWindow_LiveContentClosed;
        LanguageComboBox.SelectionChanged -= LiveLanguage_SelectionChanged;
        _liveContentTimer.Stop();
        _liveContentTimer.Tick -= LiveContentTimer_Tick;
        _serverStatusTimer.Tick -= DashboardStatusTimer_Tick;
        _liveContentCancellation?.Cancel();
        _liveContentCancellation?.Dispose();
        _liveContentCancellation = null;
        _liveContentClient?.Dispose();
        _liveContentClient = null;
    }
}
