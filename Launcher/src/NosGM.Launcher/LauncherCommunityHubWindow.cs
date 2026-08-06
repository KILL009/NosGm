// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NosGM.Launcher;

internal sealed class LauncherCommunityHubWindow : Window
{
    private sealed record CommunityText(
        string Title,
        string Subtitle,
        string Refresh,
        string News,
        string Rankings,
        string Events,
        string Combat,
        string Reputation,
        string Hero,
        string Position,
        string Character,
        string Level,
        string HeroLevel,
        string ReputationValue,
        string Score,
        string NoNews,
        string NoRanking,
        string NoEvents,
        string Loading,
        string Live,
        string Cached,
        string PortalUnavailable,
        string OpenPortal,
        string OpenNews,
        string OpenRankings,
        string Close,
        string Players,
        string Online,
        string Degraded,
        string Offline,
        string Maintenance,
        string AllChannels,
        string Channel,
        string AllLevels,
        string Levels,
        string ActiveNow,
        string Starts);

    private sealed record RankingChoice(string Id, string Name);

    private sealed record RankingDisplay(
        int Position,
        string CharacterName,
        string Level,
        string HeroLevel,
        string Reputation,
        string Score);

    private static readonly CommunityText Spanish = new(
        "Comunidad NosGM",
        "Noticias, clasificaciones y calendario público del servidor.",
        "↻ Actualizar",
        "Noticias",
        "Rankings",
        "Eventos",
        "Combate",
        "Reputación",
        "Héroe",
        "#",
        "Personaje",
        "Nivel",
        "Héroe",
        "Reputación",
        "Puntuación",
        "Todavía no hay noticias publicadas para este idioma.",
        "Todavía no hay posiciones publicadas en este ranking.",
        "No hay eventos activos o próximos en el calendario.",
        "Actualizando comunidad...",
        "Datos en vivo",
        "Datos en caché",
        "El portal de comunidad no está disponible.",
        "Abrir portal",
        "Ver noticias",
        "Ver rankings",
        "Cerrar",
        "jugadores",
        "En línea",
        "Degradado",
        "Fuera de línea",
        "MANTENIMIENTO",
        "Todos los canales",
        "Canal",
        "Todos los niveles",
        "Niveles",
        "EN CURSO",
        "Comienza");

    private static readonly CommunityText English = new(
        "NosGM Community",
        "Public server news, rankings and event calendar.",
        "↻ Refresh",
        "News",
        "Rankings",
        "Events",
        "Combat",
        "Reputation",
        "Hero",
        "#",
        "Character",
        "Level",
        "Hero",
        "Reputation",
        "Score",
        "There are no published news items for this language yet.",
        "There are no published entries in this ranking yet.",
        "There are no active or upcoming calendar events.",
        "Refreshing community...",
        "Live data",
        "Cached data",
        "The community portal is unavailable.",
        "Open portal",
        "View news",
        "View rankings",
        "Close",
        "players",
        "Online",
        "Degraded",
        "Offline",
        "MAINTENANCE",
        "All channels",
        "Channel",
        "All levels",
        "Levels",
        "LIVE NOW",
        "Starts");

    private readonly LauncherSettings _settings;
    private readonly CommunityText _text;
    private readonly LauncherCommunityClient _client;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly TextBlock _serverNameText;
    private readonly TextBlock _healthText;
    private readonly TextBlock _onlineText;
    private readonly TextBlock _freshnessText;
    private readonly TextBlock _messageText;
    private readonly Button _refreshButton;
    private readonly StackPanel _newsPanel;
    private readonly StackPanel _eventsPanel;
    private readonly ComboBox _rankingSelector;
    private readonly ListView _rankingList;

    private LauncherCommunitySnapshot? _snapshot;
    private bool _closed;

    private LauncherCommunityHubWindow(LauncherSettings settings)
    {
        _settings = settings;
        _text = string.Equals(settings.Language, "es", StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;
        _client = new LauncherCommunityClient(settings.PortalBaseUri);

        Title = _text.Title;
        Width = 980;
        Height = 700;
        MinWidth = 860;
        MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brush("#070B18");
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headerText = new StackPanel();
        headerText.Children.Add(new TextBlock
        {
            Text = _text.Title,
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });
        headerText.Children.Add(new TextBlock
        {
            Text = _text.Subtitle,
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = Brush("#9AA7C1")
        });
        header.Children.Add(headerText);

        _refreshButton = CreateButton(_text.Refresh, 118);
        _refreshButton.Click += RefreshButton_Click;
        _refreshButton.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_refreshButton, 1);
        header.Children.Add(_refreshButton);
        root.Children.Add(header);

        var summaryCard = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 12, 16, 12),
            Background = Brush("#151B31"),
            BorderBrush = Brush("#334F8BFF"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var summaryGrid = new Grid();
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _serverNameText = new TextBlock
        {
            Text = "NosGM",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold
        };
        summaryGrid.Children.Add(_serverNameText);

        _onlineText = new TextBlock
        {
            Text = "0 " + _text.Players,
            Margin = new Thickness(24, 0, 24, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#7DD3FC"),
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(_onlineText, 1);
        summaryGrid.Children.Add(_onlineText);

        _healthText = new TextBlock
        {
            Text = _text.Loading,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#FBBF24"),
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(_healthText, 2);
        summaryGrid.Children.Add(_healthText);

        _messageText = new TextBlock
        {
            Text = _text.Loading,
            Margin = new Thickness(0, 5, 0, 0),
            Foreground = Brush("#9AA7C1"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(_messageText, 1);
        summaryGrid.Children.Add(_messageText);

        _freshnessText = new TextBlock
        {
            Text = "—",
            Margin = new Thickness(24, 5, 0, 0),
            Foreground = Brush("#71809C"),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(_freshnessText, 1);
        Grid.SetColumn(_freshnessText, 1);
        Grid.SetColumnSpan(_freshnessText, 2);
        summaryGrid.Children.Add(_freshnessText);

        summaryCard.Child = summaryGrid;
        Grid.SetRow(summaryCard, 1);
        root.Children.Add(summaryCard);

        var tabs = new TabControl
        {
            Background = Brush("#0C1224"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#334F8BFF"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        _newsPanel = new StackPanel { Margin = new Thickness(8) };
        tabs.Items.Add(new TabItem
        {
            Header = _text.News,
            Content = new ScrollViewer
            {
                Content = _newsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        });

        var rankingGrid = new Grid { Margin = new Thickness(10) };
        rankingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rankingGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _rankingSelector = new ComboBox
        {
            Width = 190,
            HorizontalAlignment = HorizontalAlignment.Left,
            DisplayMemberPath = nameof(RankingChoice.Name),
            SelectedValuePath = nameof(RankingChoice.Id),
            ItemsSource = new[]
            {
                new RankingChoice("combat", _text.Combat),
                new RankingChoice("reputation", _text.Reputation),
                new RankingChoice("hero", _text.Hero)
            },
            SelectedIndex = 0,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _rankingSelector.SelectionChanged += RankingSelector_SelectionChanged;
        rankingGrid.Children.Add(_rankingSelector);

        _rankingList = CreateRankingList();
        Grid.SetRow(_rankingList, 1);
        rankingGrid.Children.Add(_rankingList);
        tabs.Items.Add(new TabItem
        {
            Header = _text.Rankings,
            Content = rankingGrid
        });

        _eventsPanel = new StackPanel { Margin = new Thickness(8) };
        tabs.Items.Add(new TabItem
        {
            Header = _text.Events,
            Content = new ScrollViewer
            {
                Content = _eventsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        });

        Grid.SetRow(tabs, 2);
        root.Children.Add(tabs);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var links = new StackPanel { Orientation = Orientation.Horizontal };
        var portalButton = CreateButton(_text.OpenPortal, 108);
        portalButton.Click += (_, _) => OpenPortalPath(string.Empty);
        var newsButton = CreateButton(_text.OpenNews, 108);
        newsButton.Click += (_, _) => OpenPortalPath(
            $"news?lang={Uri.EscapeDataString(LauncherCommunityClient.ToPortalLanguage(_settings.Language))}");
        var rankingsButton = CreateButton(_text.OpenRankings, 108);
        rankingsButton.Click += (_, _) => OpenPortalPath(
            $"rankings?lang={Uri.EscapeDataString(LauncherCommunityClient.ToPortalLanguage(_settings.Language))}");
        links.Children.Add(portalButton);
        links.Children.Add(newsButton);
        links.Children.Add(rankingsButton);
        footer.Children.Add(links);

        var closeButton = CreateButton(_text.Close, 96);
        closeButton.IsCancel = true;
        closeButton.Click += (_, _) => Close();
        Grid.SetColumn(closeButton, 1);
        footer.Children.Add(closeButton);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        Content = root;
        Loaded += Window_Loaded;
        Closed += Window_Closed;
    }

    public static void Show(Window owner, LauncherSettings settings)
    {
        var window = new LauncherCommunityHubWindow(settings)
        {
            Owner = owner
        };
        window.ShowDialog();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var cached = await LauncherCommunityCache.LoadAsync(_lifetime.Token);
            if (_closed || _lifetime.IsCancellationRequested)
            {
                return;
            }

            if (cached is not null)
            {
                ApplySnapshot(cached, fromCache: true);
            }

            await RefreshAsync();
        }
        catch (OperationCanceledException) when (_closed || _lifetime.IsCancellationRequested)
        {
            // Normal close while cached public data is being read.
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_closed || _lifetime.IsCancellationRequested)
        {
            return;
        }

        bool entered;
        try
        {
            entered = await _refreshGate.WaitAsync(0, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_closed || _lifetime.IsCancellationRequested)
        {
            return;
        }

        if (!entered)
        {
            return;
        }

        try
        {
            if (_closed)
            {
                return;
            }

            _refreshButton.IsEnabled = false;
            _messageText.Text = _text.Loading;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            var snapshot = await _client.GetSnapshotAsync(_settings.Language, timeout.Token);
            if (_closed || _lifetime.IsCancellationRequested)
            {
                return;
            }

            ApplySnapshot(snapshot, fromCache: false);
            try
            {
                await LauncherCommunityCache.SaveAsync(snapshot, CancellationToken.None);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                // Valid public data remains visible even when optional cache persistence fails.
            }
        }
        catch (OperationCanceledException) when (_closed || _lifetime.IsCancellationRequested)
        {
            // Normal close while a portal request is active.
        }
        catch (OperationCanceledException)
        {
            MarkUnavailable();
        }
        catch (Exception) when (_closed || _lifetime.IsCancellationRequested)
        {
            // Disposing the HTTP client during close is an expected lifecycle event.
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or JsonException or InvalidDataException)
        {
            MarkUnavailable();
        }
        finally
        {
            if (!_closed)
            {
                _refreshButton.IsEnabled = true;
            }

            _refreshGate.Release();
        }
    }

    private void ApplySnapshot(LauncherCommunitySnapshot snapshot, bool fromCache)
    {
        LauncherCommunityValidator.Validate(snapshot);
        _snapshot = snapshot;
        _serverNameText.Text = snapshot.Status.ServerName;
        _onlineText.Text = $"{snapshot.Status.OnlinePlayers:N0} {_text.Players}";

        var health = snapshot.Status.OverallHealth;
        _healthText.Text = health switch
        {
            LauncherServiceHealth.Online => _text.Online,
            LauncherServiceHealth.Degraded => _text.Degraded,
            _ => _text.Offline
        };
        _healthText.Foreground = health switch
        {
            LauncherServiceHealth.Online => Brush("#3EE88F"),
            LauncherServiceHealth.Degraded => Brush("#FBBF24"),
            _ => Brush("#FF5D7A")
        };

        var cached = fromCache || snapshot.Status.IsStale;
        _freshnessText.Text =
            $"{(cached ? _text.Cached : _text.Live)} • {snapshot.FetchedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}";
        _messageText.Text = snapshot.Maintenance.IsActive
            ? BuildMaintenanceSummary(snapshot.Maintenance)
            : cached
                ? _text.Cached
                : _text.Live;

        RenderNews(snapshot.News);
        RenderEvents(snapshot.Maintenance, snapshot.Events);
        ApplyRanking();
    }

    private void RenderNews(IReadOnlyList<LauncherNewsItem> news)
    {
        _newsPanel.Children.Clear();
        var items = news.OrderByDescending(item => item.PublishedAt).ToArray();
        if (items.Length == 0)
        {
            _newsPanel.Children.Add(CreatePlaceholder(_text.NoNews));
            return;
        }

        foreach (var item in items)
        {
            var content = new StackPanel();
            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            var date = new TextBlock
            {
                Text = item.PublishedAt.ToLocalTime().ToString("dd/MM/yyyy"),
                Margin = new Thickness(16, 0, 0, 0),
                Foreground = Brush("#7DD3FC")
            };
            Grid.SetColumn(date, 1);
            titleGrid.Children.Add(date);
            content.Children.Add(titleGrid);
            content.Children.Add(new TextBlock
            {
                Text = item.Summary,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = Brush("#A8B3CC"),
                TextWrapping = TextWrapping.Wrap
            });
            _newsPanel.Children.Add(CreateCard(content));
        }
    }

    private void RenderEvents(
        LauncherMaintenanceStatus maintenance,
        IReadOnlyList<LauncherCalendarEvent> events)
    {
        _eventsPanel.Children.Clear();
        var now = DateTimeOffset.UtcNow;
        var maintenanceUpcoming = maintenance.StartsAt is { } startsAt && startsAt > now;

        if (maintenance.IsActive || maintenanceUpcoming)
        {
            var maintenanceContent = new StackPanel();
            maintenanceContent.Children.Add(new TextBlock
            {
                Text = _text.Maintenance + (string.IsNullOrWhiteSpace(maintenance.Title)
                    ? string.Empty
                    : " • " + maintenance.Title),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brush("#FBBF24")
            });
            if (!string.IsNullOrWhiteSpace(maintenance.Message))
            {
                maintenanceContent.Children.Add(new TextBlock
                {
                    Text = maintenance.Message,
                    Margin = new Thickness(0, 7, 0, 0),
                    Foreground = Brush("#D9C79B"),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            maintenanceContent.Children.Add(new TextBlock
            {
                Text = BuildMaintenanceSummary(maintenance),
                Margin = new Thickness(0, 7, 0, 0),
                Foreground = Brush("#9AA7C1")
            });
            _eventsPanel.Children.Add(CreateCard(maintenanceContent, "#554121"));
        }

        var upcoming = events
            .Where(item => item.EndsAt >= now.AddMinutes(-1))
            .OrderBy(item => item.StartsAt)
            .ToArray();
        foreach (var item in upcoming)
        {
            var active = item.StartsAt <= now && item.EndsAt > now;
            var content = new StackPanel();
            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            var timing = new TextBlock
            {
                Text = active
                    ? _text.ActiveNow
                    : $"{_text.Starts} {item.StartsAt.ToLocalTime():dd/MM HH:mm}",
                Margin = new Thickness(16, 0, 0, 0),
                Foreground = active ? Brush("#3EE88F") : Brush("#7DD3FC"),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(timing, 1);
            titleGrid.Children.Add(timing);
            content.Children.Add(titleGrid);

            var channel = item.Channel == 0
                ? _text.AllChannels
                : $"{_text.Channel} {item.Channel}";
            var levels = item.MinimumLevel == 0 && item.MaximumLevel == 0
                ? _text.AllLevels
                : $"{_text.Levels} {item.MinimumLevel}-{item.MaximumLevel}";
            content.Children.Add(new TextBlock
            {
                Text = $"{item.Category} • {channel} • {levels} • {item.StartsAt.ToLocalTime():dd/MM HH:mm} - {item.EndsAt.ToLocalTime():dd/MM HH:mm}",
                Margin = new Thickness(0, 7, 0, 0),
                Foreground = Brush("#9AA7C1"),
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(item.Details))
            {
                content.Children.Add(new TextBlock
                {
                    Text = item.Details,
                    Margin = new Thickness(0, 7, 0, 0),
                    Foreground = Brush("#A8B3CC"),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            _eventsPanel.Children.Add(CreateCard(content));
        }

        if (_eventsPanel.Children.Count == 0)
        {
            _eventsPanel.Children.Add(CreatePlaceholder(_text.NoEvents));
        }
    }

    private void RankingSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ApplyRanking();

    private void ApplyRanking()
    {
        if (_snapshot is null)
        {
            _rankingList.ItemsSource = Array.Empty<RankingDisplay>();
            return;
        }

        var selected = _rankingSelector.SelectedValue as string ?? "combat";
        var entries = selected switch
        {
            "reputation" => _snapshot.ReputationRanking,
            "hero" => _snapshot.HeroRanking,
            _ => _snapshot.CombatRanking
        };
        var rows = entries
            .OrderBy(entry => entry.Position)
            .Select(entry => new RankingDisplay(
                entry.Position,
                entry.CharacterName,
                entry.Level.ToString("N0"),
                entry.HeroLevel.ToString("N0"),
                entry.Reputation.ToString("N0"),
                entry.Score.ToString("N0")))
            .ToArray();
        _rankingList.ItemsSource = rows.Length == 0
            ? new[] { new RankingDisplay(0, _text.NoRanking, "—", "—", "—", "—") }
            : rows;
    }

    private ListView CreateRankingList()
    {
        var list = new ListView
        {
            Background = Brush("#0C1224"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        var view = new GridView();
        view.Columns.Add(Column(_text.Position, nameof(RankingDisplay.Position), 55));
        view.Columns.Add(Column(_text.Character, nameof(RankingDisplay.CharacterName), 240));
        view.Columns.Add(Column(_text.Level, nameof(RankingDisplay.Level), 90));
        view.Columns.Add(Column(_text.HeroLevel, nameof(RankingDisplay.HeroLevel), 90));
        view.Columns.Add(Column(_text.ReputationValue, nameof(RankingDisplay.Reputation), 150));
        view.Columns.Add(Column(_text.Score, nameof(RankingDisplay.Score), 150));
        list.View = view;
        return list;
    }

    private static GridViewColumn Column(string header, string property, double width)
        => new()
        {
            Header = header,
            Width = width,
            DisplayMemberBinding = new Binding(property)
        };

    private void MarkUnavailable()
    {
        if (_closed)
        {
            return;
        }

        _messageText.Text = _snapshot is null
            ? _text.PortalUnavailable
            : _text.PortalUnavailable + " • " + _text.Cached;
        if (_snapshot is null)
        {
            _healthText.Text = _text.Offline;
            _healthText.Foreground = Brush("#FF5D7A");
        }
    }

    private string BuildMaintenanceSummary(LauncherMaintenanceStatus maintenance)
    {
        if (maintenance.IsActive)
        {
            return maintenance.EndsAt is null
                ? _text.Maintenance
                : $"{_text.Maintenance} • {_text.ActiveNow} • {maintenance.EndsAt.Value.ToLocalTime():dd/MM HH:mm}";
        }

        return maintenance.StartsAt is null
            ? _text.Maintenance
            : $"{_text.Maintenance} • {_text.Starts} {maintenance.StartsAt.Value.ToLocalTime():dd/MM HH:mm}";
    }

    private void OpenPortalPath(string relativePath)
    {
        try
        {
            var baseUri = new Uri(_settings.PortalBaseUri, UriKind.Absolute);
            var target = new Uri(baseUri, relativePath);
            if (!string.Equals(target.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(target.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
                || target.Port != baseUri.Port)
            {
                throw new InvalidDataException("Community link escaped the configured portal origin.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = target.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or InvalidDataException or UriFormatException
                or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                _text.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static Border CreateCard(UIElement content, string background = "#151B31")
        => new()
        {
            Child = content,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
            Background = Brush(background),
            BorderBrush = Brush("#263B68"),
            BorderThickness = new Thickness(1)
        };

    private static Border CreatePlaceholder(string text)
        => CreateCard(new TextBlock
        {
            Text = text,
            Foreground = Brush("#9AA7C1"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(12)
        });

    private Button CreateButton(string text, double minimumWidth)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = minimumWidth,
            Height = 36,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12, 5, 12, 5)
        };
        if (TryFindResource("NeonButton") is Style style)
        {
            button.Style = style;
        }

        return button;
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        Loaded -= Window_Loaded;
        Closed -= Window_Closed;
        _refreshButton.Click -= RefreshButton_Click;
        _rankingSelector.SelectionChanged -= RankingSelector_SelectionChanged;
        _lifetime.Cancel();
        _ = DisposeResourcesAfterRefreshAsync();
    }

    private async Task DisposeResourcesAfterRefreshAsync()
    {
        try
        {
            await _refreshGate.WaitAsync().ConfigureAwait(false);
            _client.Dispose();
            _lifetime.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Defensive only; cleanup is intentionally idempotent.
        }
        finally
        {
            try
            {
                _refreshGate.Release();
                _refreshGate.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Another defensive cleanup path already completed.
            }
        }
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}
