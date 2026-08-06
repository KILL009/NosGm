// SPDX-License-Identifier: MIT

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NosGM.Launcher;

internal sealed class LauncherAccountHubWindow : Window
{
    private sealed record AccountHubText(
        string Title,
        string Subtitle,
        string CurrentAccount,
        string NoAccount,
        string ModernAuthentication,
        string ClassicAuthentication,
        string SecurityNote,
        string RecentAccounts,
        string EmptyHistory,
        string UseSelected,
        string ForgetSelected,
        string UseAnother,
        string Close,
        string SelectRequired);

    private static readonly AccountHubText Spanish = new(
        "Mi cuenta",
        "Administra qué cuenta aparecerá preparada al pulsar JUGAR.",
        "Cuenta preparada",
        "Ninguna cuenta preparada",
        "Autenticación moderna",
        "Modo clásico",
        "NosGM solo conserva nombres de cuenta. La contraseña y los tickets de acceso nunca se guardan.",
        "CUENTAS RECIENTES",
        "Todavía no hay cuentas recientes. Inicia el juego una vez para añadir una.",
        "Usar seleccionada",
        "Olvidar seleccionada",
        "Usar otra cuenta",
        "Cerrar",
        "Selecciona una cuenta primero.");

    private static readonly AccountHubText English = new(
        "My account",
        "Choose which account is prepared when you press PLAY.",
        "Prepared account",
        "No prepared account",
        "Modern authentication",
        "Classic mode",
        "NosGM stores account names only. Passwords and access tickets are never saved.",
        "RECENT ACCOUNTS",
        "There are no recent accounts yet. Start the game once to add one.",
        "Use selected",
        "Forget selected",
        "Use another account",
        "Close",
        "Select an account first.");

    private readonly AccountHubText _text;
    private readonly LauncherSettings _originalSettings;
    private readonly ListBox _accountsList;
    private readonly Button _useSelectedButton;
    private readonly Button _forgetSelectedButton;

    private LauncherAccountHubWindow(LauncherSettings settings)
    {
        _originalSettings = LauncherAccountHistory.Normalize(settings);
        _text = string.Equals(settings.Language, "es", StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;

        Title = _text.Title;
        Width = 620;
        Height = 500;
        MinWidth = 560;
        MinHeight = 450;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brush("#070B18");
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new TextBlock
        {
            Text = _text.Title,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });
        header.Children.Add(new TextBlock
        {
            Text = _text.Subtitle,
            Margin = new Thickness(0, 5, 0, 0),
            Foreground = Brush("#9AA7C1"),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var currentCard = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Background = Brush("#151B31"),
            BorderBrush = Brush("#334F8BFF"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var currentGrid = new Grid();
        currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        currentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        currentGrid.Children.Add(new TextBlock
        {
            Text = "👤",
            FontSize = 30,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        });

        var currentText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        currentText.Children.Add(new TextBlock
        {
            Text = _text.CurrentAccount,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#7DD3FC")
        });
        currentText.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(_originalSettings.AccountName)
                ? _text.NoAccount
                : _originalSettings.AccountName,
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(currentText, 1);
        currentGrid.Children.Add(currentText);

        var authenticationBadge = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 5, 10, 5),
            Background = Brush("#3029C7FF"),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_originalSettings.AuthenticationEndpoint)
                    ? _text.ClassicAuthentication
                    : _text.ModernAuthentication,
                Foreground = Brush("#B9E6FF"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            }
        };
        Grid.SetColumn(authenticationBadge, 2);
        currentGrid.Children.Add(authenticationBadge);
        currentCard.Child = currentGrid;
        Grid.SetRow(currentCard, 1);
        root.Children.Add(currentCard);

        var securityNote = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Background = Brush("#2210B981"),
            BorderBrush = Brush("#3340E0A0"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = "🔒 " + _text.SecurityNote,
                Foreground = Brush("#B9FBCF"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11
            }
        };
        Grid.SetRow(securityNote, 2);
        root.Children.Add(securityNote);

        var recentLabel = new TextBlock
        {
            Text = _text.RecentAccounts,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#C084FC"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(recentLabel, 3);
        root.Children.Add(recentLabel);

        var historyPanel = new Grid();
        _accountsList = new ListBox
        {
            Background = Brush("#10162A"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#334F8BFF"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6),
            FontSize = 14
        };
        foreach (var accountName in _originalSettings.RecentAccountNames)
        {
            _accountsList.Items.Add(accountName);
        }

        if (_accountsList.Items.Count > 0)
        {
            _accountsList.SelectedItem = _accountsList.Items
                .Cast<string>()
                .FirstOrDefault(accountName => string.Equals(
                    accountName,
                    _originalSettings.AccountName,
                    StringComparison.OrdinalIgnoreCase)) ?? _accountsList.Items[0];
        }

        _accountsList.SelectionChanged += (_, _) => UpdateSelectionButtons();
        historyPanel.Children.Add(_accountsList);

        var emptyHistory = new TextBlock
        {
            Text = _text.EmptyHistory,
            Foreground = Brush("#7F8CA7"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(40),
            Visibility = _accountsList.Items.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed
        };
        historyPanel.Children.Add(emptyHistory);
        Grid.SetRow(historyPanel, 4);
        root.Children.Add(historyPanel);

        var buttons = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _forgetSelectedButton = CreateButton(_text.ForgetSelected, secondary: true);
        _forgetSelectedButton.Click += ForgetSelected_Click;
        Grid.SetColumn(_forgetSelectedButton, 0);
        buttons.Children.Add(_forgetSelectedButton);

        var useAnotherButton = CreateButton(_text.UseAnother, secondary: true);
        useAnotherButton.Margin = new Thickness(8, 0, 0, 0);
        useAnotherButton.Click += UseAnother_Click;
        Grid.SetColumn(useAnotherButton, 1);
        buttons.Children.Add(useAnotherButton);

        var closeButton = CreateButton(_text.Close, secondary: true);
        closeButton.IsCancel = true;
        Grid.SetColumn(closeButton, 3);
        buttons.Children.Add(closeButton);

        _useSelectedButton = CreateButton(_text.UseSelected, secondary: false);
        _useSelectedButton.Margin = new Thickness(8, 0, 0, 0);
        _useSelectedButton.IsDefault = true;
        _useSelectedButton.Click += UseSelected_Click;
        Grid.SetColumn(_useSelectedButton, 4);
        buttons.Children.Add(_useSelectedButton);

        Grid.SetRow(buttons, 5);
        root.Children.Add(buttons);
        Content = root;

        UpdateSelectionButtons();
    }

    public LauncherSettings? UpdatedSettings { get; private set; }

    public static LauncherSettings? Prompt(Window owner, LauncherSettings settings)
    {
        var window = new LauncherAccountHubWindow(settings)
        {
            Owner = owner
        };
        return window.ShowDialog() == true ? window.UpdatedSettings : null;
    }

    private void UseSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_accountsList.SelectedItem is not string accountName)
        {
            MessageBox.Show(
                this,
                _text.SelectRequired,
                _text.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        UpdatedSettings = LauncherAccountHistory.Select(_originalSettings, accountName);
        DialogResult = true;
    }

    private void ForgetSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_accountsList.SelectedItem is not string accountName)
        {
            MessageBox.Show(
                this,
                _text.SelectRequired,
                _text.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        UpdatedSettings = LauncherAccountHistory.Forget(_originalSettings, accountName);
        DialogResult = true;
    }

    private void UseAnother_Click(object sender, RoutedEventArgs e)
    {
        UpdatedSettings = LauncherAccountHistory.UseAnotherAccount(_originalSettings);
        DialogResult = true;
    }

    private void UpdateSelectionButtons()
    {
        var hasSelection = _accountsList.SelectedItem is string;
        _useSelectedButton.IsEnabled = hasSelection;
        _forgetSelectedButton.IsEnabled = hasSelection;
    }

    private static Button CreateButton(string text, bool secondary)
    {
        return new Button
        {
            Content = text,
            MinWidth = 112,
            Padding = new Thickness(13, 8, 13, 8),
            Background = Brush(secondary ? "#1A2138" : "#6D4AFF"),
            Foreground = Brushes.White,
            BorderBrush = Brush(secondary ? "#43506E" : "#9B87FF"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }

    private static SolidColorBrush Brush(string value)
        => (SolidColorBrush)new BrushConverter().ConvertFromString(value)!;
}
