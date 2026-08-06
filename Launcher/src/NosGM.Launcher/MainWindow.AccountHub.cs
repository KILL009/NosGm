// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace NosGM.Launcher;

internal static class LauncherAccountHubModule
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
            window.InitializeAccountHub();
        }
    }
}

public partial class MainWindow
{
    private readonly CancellationTokenSource _accountHubLifetime = new();
    private Button? _accountHubButton;
    private bool _accountHubInitialized;

    internal void InitializeAccountHub()
    {
        if (_accountHubInitialized)
        {
            return;
        }

        if (LanguageComboBox.Parent is not StackPanel languagePanel)
        {
            return;
        }

        _accountHubInitialized = true;
        _accountHubButton = new Button
        {
            MinWidth = 118,
            MaxWidth = 180,
            Height = 32,
            Margin = new Thickness(0, 0, 14, 0),
            Padding = new Thickness(10, 4, 10, 4),
            Content = "👤 Mi cuenta",
            Style = TryFindResource("NeonButton") as Style,
            ToolTip = "NosGM no guarda contraseñas ni tickets de acceso."
        };
        _accountHubButton.Click += AccountHubButton_Click;
        WindowChrome.SetIsHitTestVisibleInChrome(_accountHubButton, true);
        languagePanel.Children.Insert(0, _accountHubButton);

        ModernGameLauncher.GameLaunched += AccountHub_GameLaunched;
        Closed += MainWindow_AccountHubClosed;
        PrepareAccountHubAfterSettingsLoad();
    }

    private async void PrepareAccountHubAfterSettingsLoad()
    {
        try
        {
            for (var attempt = 0;
                 attempt < 100 && !_languageSelectionReady && IsLoaded;
                 attempt++)
            {
                await Task.Delay(
                        TimeSpan.FromMilliseconds(50),
                        _accountHubLifetime.Token)
                    .ConfigureAwait(true);
            }

            if (!IsLoaded ||
                !_languageSelectionReady ||
                _accountHubLifetime.IsCancellationRequested)
            {
                return;
            }

            var normalized = LauncherAccountHistory.Normalize(_settings);
            if (!LauncherAccountHistory.StoredAccountsEqual(_settings, normalized))
            {
                _settings = normalized;
                await LauncherSettingsStore.SaveAsync(_settings).ConfigureAwait(true);
            }

            RefreshAccountHubButton();
        }
        catch (OperationCanceledException) when (_accountHubLifetime.IsCancellationRequested)
        {
            // Normal launcher shutdown.
        }
        catch
        {
            // Account conveniences cannot block launcher startup.
        }
    }

    private async void AccountHubButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var updatedSettings = LauncherAccountHubWindow.Prompt(this, _settings);
            if (updatedSettings is null)
            {
                return;
            }

            _settings = LauncherAccountHistory.Normalize(updatedSettings);
            await LauncherSettingsStore.SaveAsync(_settings).ConfigureAwait(true);
            RefreshAccountHubButton();
            DetailTextBlock.Text = string.IsNullOrWhiteSpace(_settings.AccountName)
                ? AccountHubMessage("La próxima vez podrás escribir otra cuenta.", "You can enter another account next time.")
                : AccountHubMessage(
                    $"Cuenta preparada: {_settings.AccountName}. La contraseña se pedirá al jugar.",
                    $"Prepared account: {_settings.AccountName}. The password will be requested when playing.");
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void AccountHub_GameLaunched(Process process, string accountName)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AccountHub_GameLaunched(process, accountName));
            return;
        }

        // ModernGameLauncher raises this event synchronously before Play_Click
        // persists the successful account. Updating _settings here makes the
        // existing single settings write include the bounded history too.
        _settings = LauncherAccountHistory.Remember(_settings, accountName);
        RefreshAccountHubButton();
    }

    private void RefreshAccountHubButton()
    {
        if (_accountHubButton is null)
        {
            return;
        }

        var accountName = _settings.AccountName;
        _accountHubButton.Content = string.IsNullOrWhiteSpace(accountName)
            ? AccountHubMessage("👤 Mi cuenta", "👤 My account")
            : "👤 " + LimitAccountDisplay(accountName, 22);
        _accountHubButton.ToolTip = AccountHubMessage(
            "Administra nombres de cuenta. NosGM no guarda contraseñas ni tickets.",
            "Manage account names. NosGM never stores passwords or tickets.");
    }

    private string AccountHubMessage(string spanish, string english)
        => string.Equals(_settings.Language, "es", StringComparison.OrdinalIgnoreCase)
            ? spanish
            : english;

    private static string LimitAccountDisplay(string value, int maximumCharacters)
        => value.Length <= maximumCharacters
            ? value
            : value[..(maximumCharacters - 1)] + "…";

    private void MainWindow_AccountHubClosed(object? sender, EventArgs e)
    {
        Closed -= MainWindow_AccountHubClosed;
        ModernGameLauncher.GameLaunched -= AccountHub_GameLaunched;
        _accountHubButton?.Click -= AccountHubButton_Click;
        _accountHubLifetime.Cancel();
        _accountHubLifetime.Dispose();
    }
}
