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
    private bool _localRepairChannelBootstrapStarted;

    internal async void StartLocalRepairChannelBootstrap()
    {
        if (_localRepairChannelBootstrapStarted)
        {
            return;
        }

        _localRepairChannelBootstrapStarted = true;
        for (var attempt = 0;
             attempt < 100 && !_languageSelectionReady && IsLoaded;
             attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), _lifetime.Token)
                .ConfigureAwait(true);
        }

        if (!IsLoaded || !_languageSelectionReady || _lifetime.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _ = await LocalDevelopmentRepairChannel.EnsureAsync(
                    _settings,
                    _lifetime.Token)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Normal launcher shutdown while the local channel is being prepared.
        }
        catch
        {
            // The local development channel is optional. Diagnostics and play stay available.
        }
    }
}
