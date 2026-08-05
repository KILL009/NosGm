// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Windows;

namespace NosGM.Launcher;

internal static class LocalRepairChannelModule
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
            window.StartLocalRepairChannelBootstrap();
        }
    }
}

public partial class MainWindow
{
    private readonly CancellationTokenSource _localRepairChannelLifetime = new();
    private bool _localRepairChannelBootstrapStarted;

    internal async void StartLocalRepairChannelBootstrap()
    {
        if (_localRepairChannelBootstrapStarted)
        {
            return;
        }

        _localRepairChannelBootstrapStarted = true;
        Closed += MainWindow_LocalRepairChannelClosed;
        for (var attempt = 0;
             attempt < 100 && !_languageSelectionReady && IsLoaded;
             attempt++)
        {
            await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    _localRepairChannelLifetime.Token)
                .ConfigureAwait(true);
        }

        if (!IsLoaded ||
            !_languageSelectionReady ||
            _localRepairChannelLifetime.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _ = await LocalDevelopmentRepairChannel.EnsureAsync(
                    _settings,
                    _localRepairChannelLifetime.Token)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (
            _localRepairChannelLifetime.IsCancellationRequested)
        {
            // Normal launcher shutdown while the local channel is being prepared.
        }
        catch
        {
            // The local development channel is optional. Diagnostics and play stay available.
        }
    }

    private void MainWindow_LocalRepairChannelClosed(object? sender, EventArgs e)
    {
        Closed -= MainWindow_LocalRepairChannelClosed;
        _localRepairChannelLifetime.Cancel();
    }
}
