// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NosGM.Launcher;

internal static class LauncherDiagnosticsModule
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
            window.InitializeDiagnosticsCenterButton();
        }
    }
}

public partial class MainWindow
{
    private bool _diagnosticsButtonInitialized;

    internal void InitializeDiagnosticsCenterButton()
    {
        if (_diagnosticsButtonInitialized)
        {
            return;
        }

        var supportButton = EnumerateVisualDescendants<Button>(this)
            .FirstOrDefault(button =>
                button.Content is string content
                && content.Contains("Soporte", StringComparison.OrdinalIgnoreCase));
        if (supportButton is null)
        {
            return;
        }

        _diagnosticsButtonInitialized = true;
        supportButton.Click -= OpenExternalLink_Click;
        supportButton.Click += OpenDiagnosticsCenter_Click;
        supportButton.Content = "🛠 Diagnóstico";
        supportButton.Tag = null;
        supportButton.ToolTip =
            "Comprueba instalación, red, servicios y crea un ZIP sanitizado para soporte.";
    }

    private async void OpenDiagnosticsCenter_Click(object sender, RoutedEventArgs e)
    {
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
            return;
        }
        catch
        {
            // Diagnostics remains useful even when the optional source-build
            // repair channel cannot be prepared.
        }

        var window = new LauncherDiagnosticsWindow(_settings)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private static IEnumerable<T> EnumerateVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in EnumerateVisualDescendants<T>(child))
            {
                yield return nested;
            }
        }
    }
}
