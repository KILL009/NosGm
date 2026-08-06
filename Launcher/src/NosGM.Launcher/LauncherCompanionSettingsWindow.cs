// SPDX-License-Identifier: MIT

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NosGM.Launcher;

internal sealed class LauncherCompanionSettingsWindow : Window
{
    private sealed record CompanionText(
        string Title,
        string Subtitle,
        string Companion,
        string CompanionDetail,
        string Restore,
        string Events,
        string EventsDetail,
        string Maintenance,
        string Reminder,
        string Minutes,
        string Privacy,
        string Test,
        string Save,
        string Cancel);

    private static readonly CompanionText Spanish = new(
        "NosGM Companion",
        "Mantén el launcher vivo mientras juegas y recibe avisos del calendario público.",
        "Mantener NosGM en la bandeja al iniciar el juego",
        "Conserva Discord Rich Presence, el calendario y las alertas mientras el cliente está abierto.",
        "Volver a mostrar el launcher cuando termine el juego",
        "Avisarme de eventos",
        "Muestra un aviso antes de que comiencen raids, batallas y otros eventos publicados.",
        "Avisarme de mantenimientos",
        "Recordar con anticipación",
        "minutos",
        "Solo se guardan preferencias y claves públicas de avisos. Nunca cuentas, contraseñas, tickets, chat ni datos del personaje.",
        "Probar aviso",
        "Guardar",
        "Cancelar");

    private static readonly CompanionText English = new(
        "NosGM Companion",
        "Keep the launcher alive while playing and receive public calendar alerts.",
        "Keep NosGM in the tray after starting the game",
        "Keeps Discord Rich Presence, the calendar and alerts alive while the client is open.",
        "Restore the launcher when the game exits",
        "Notify me about events",
        "Shows an alert before published raids, battles and other events begin.",
        "Notify me about maintenance",
        "Remind me",
        "minutes before",
        "Only preferences and public alert keys are stored. Accounts, passwords, tickets, chat and character data are never saved.",
        "Test alert",
        "Save",
        "Cancel");

    private readonly LauncherSettings _settings;
    private readonly CompanionText _text;
    private readonly Action _testNotification;
    private readonly CheckBox _companionCheckBox;
    private readonly CheckBox _restoreCheckBox;
    private readonly CheckBox _eventsCheckBox;
    private readonly CheckBox _maintenanceCheckBox;
    private readonly ComboBox _reminderComboBox;

    private LauncherCompanionSettingsWindow(
        LauncherSettings settings,
        Action testNotification)
    {
        _settings = settings;
        _testNotification = testNotification;
        _text = string.Equals(settings.Language, "es", StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;

        Title = _text.Title;
        Width = 650;
        Height = 570;
        MinWidth = 600;
        MinHeight = 530;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Brush("#070B18");
        Foreground = Brushes.White;

        var root = new Grid { Margin = new Thickness(26) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new TextBlock
        {
            Text = _text.Title,
            FontSize = 30,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = _text.Subtitle,
            Margin = new Thickness(0, 5, 0, 0),
            Foreground = Brush("#9AA7C1"),
            TextWrapping = TextWrapping.Wrap
        });
        root.Children.Add(header);

        var content = new StackPanel();
        _companionCheckBox = CreateCheckBox(
            _text.Companion,
            settings.CompanionModeEnabled);
        _companionCheckBox.Checked += CompanionSelectionChanged;
        _companionCheckBox.Unchecked += CompanionSelectionChanged;
        content.Children.Add(CreateSettingCard(
            _companionCheckBox,
            _text.CompanionDetail));

        _restoreCheckBox = CreateCheckBox(
            _text.Restore,
            settings.CompanionRestoreAfterGame);
        content.Children.Add(CreateSettingCard(_restoreCheckBox, null));

        _eventsCheckBox = CreateCheckBox(
            _text.Events,
            settings.EventAlertsEnabled);
        content.Children.Add(CreateSettingCard(
            _eventsCheckBox,
            _text.EventsDetail));

        _maintenanceCheckBox = CreateCheckBox(
            _text.Maintenance,
            settings.MaintenanceAlertsEnabled);
        content.Children.Add(CreateSettingCard(_maintenanceCheckBox, null));

        var reminderGrid = new Grid();
        reminderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        reminderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        reminderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        reminderGrid.Children.Add(new TextBlock
        {
            Text = _text.Reminder,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        });
        _reminderComboBox = new ComboBox
        {
            Width = 88,
            ItemsSource = new[] { 5, 10, 15, 30, 60 },
            SelectedItem = settings.EventReminderMinutes,
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_reminderComboBox, 1);
        reminderGrid.Children.Add(_reminderComboBox);
        var minuteText = new TextBlock
        {
            Text = _text.Minutes,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush("#9AA7C1")
        };
        Grid.SetColumn(minuteText, 2);
        reminderGrid.Children.Add(minuteText);
        content.Children.Add(CreateCard(reminderGrid));

        content.Children.Add(new Border
        {
            Background = Brush("#102A35"),
            BorderBrush = Brush("#245B6D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 8, 0, 0),
            Child = new TextBlock
            {
                Text = _text.Privacy,
                Foreground = Brush("#A5E8F7"),
                TextWrapping = TextWrapping.Wrap
            }
        });

        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var footer = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var testButton = CreateButton(_text.Test, 116);
        testButton.Click += (_, _) => _testNotification();
        footer.Children.Add(testButton);

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var saveButton = CreateButton(_text.Save, 104);
        saveButton.Click += SaveButton_Click;
        var cancelButton = CreateButton(_text.Cancel, 104);
        cancelButton.IsCancel = true;
        cancelButton.Click += (_, _) => Close();
        actions.Children.Add(saveButton);
        actions.Children.Add(cancelButton);
        Grid.SetColumn(actions, 1);
        footer.Children.Add(actions);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
        UpdateEnabledState();
    }

    public LauncherSettings? Result { get; private set; }

    public static LauncherSettings? Show(
        Window owner,
        LauncherSettings settings,
        Action testNotification)
    {
        var window = new LauncherCompanionSettingsWindow(settings, testNotification)
        {
            Owner = owner
        };
        return window.ShowDialog() == true ? window.Result : null;
    }

    private void CompanionSelectionChanged(object sender, RoutedEventArgs e)
        => UpdateEnabledState();

    private void UpdateEnabledState()
    {
        var enabled = _companionCheckBox.IsChecked == true;
        _restoreCheckBox.IsEnabled = enabled;
        _eventsCheckBox.IsEnabled = enabled;
        _maintenanceCheckBox.IsEnabled = enabled;
        _reminderComboBox.IsEnabled = enabled &&
                                      (_eventsCheckBox.IsChecked == true ||
                                       _maintenanceCheckBox.IsChecked == true);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var reminder = _reminderComboBox.SelectedItem is int value ? value : 10;
        Result = _settings with
        {
            CompanionModeEnabled = _companionCheckBox.IsChecked == true,
            CompanionRestoreAfterGame = _restoreCheckBox.IsChecked == true,
            EventAlertsEnabled = _eventsCheckBox.IsChecked == true,
            MaintenanceAlertsEnabled = _maintenanceCheckBox.IsChecked == true,
            EventReminderMinutes = reminder
        };
        DialogResult = true;
        Close();
    }

    private static CheckBox CreateCheckBox(string text, bool isChecked)
        => new()
        {
            Content = text,
            IsChecked = isChecked,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };

    private static Border CreateSettingCard(CheckBox checkBox, string? detail)
    {
        var panel = new StackPanel();
        panel.Children.Add(checkBox);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            panel.Children.Add(new TextBlock
            {
                Text = detail,
                Margin = new Thickness(24, 6, 0, 0),
                Foreground = Brush("#9AA7C1"),
                TextWrapping = TextWrapping.Wrap
            });
        }

        return CreateCard(panel);
    }

    private static Border CreateCard(UIElement content)
        => new()
        {
            Child = content,
            Background = Brush("#151B31"),
            BorderBrush = Brush("#263B68"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };

    private Button CreateButton(string text, double minimumWidth)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = minimumWidth,
            Height = 38,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(14, 5, 14, 5)
        };
        if (TryFindResource("NeonButton") is Style style)
        {
            button.Style = style;
        }

        return button;
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}
