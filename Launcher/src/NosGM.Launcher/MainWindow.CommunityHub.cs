// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NosGM.Launcher;

internal static class LauncherCommunityHubModule
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
            window.InitializeCommunityHub();
        }
    }
}

public partial class MainWindow
{
    private Button? _communityHubButton;
    private bool _communityHubInitialized;

    internal void InitializeCommunityHub()
    {
        if (_communityHubInitialized)
        {
            return;
        }

        var forumButton = FindVisualChildren<Button>(this)
            .FirstOrDefault(button =>
                button.Content is string content
                && (content.Contains("Foro", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("Forum", StringComparison.OrdinalIgnoreCase)));
        if (forumButton is null)
        {
            return;
        }

        _communityHubInitialized = true;
        _communityHubButton = forumButton;
        _communityHubButton.Click -= OpenExternalLink_Click;
        _communityHubButton.Click += OpenCommunityHub_Click;
        _communityHubButton.Tag = null;
        _communityHubButton.ToolTip =
            "Noticias, rankings y calendario público de NosGM.";
        RefreshCommunityHubButtonText();

        LanguageComboBox.SelectionChanged += CommunityHubLanguage_SelectionChanged;
        Closed += MainWindow_CommunityHubClosed;
    }

    private void CommunityHubLanguage_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
        => RefreshCommunityHubButtonText();

    private void RefreshCommunityHubButtonText()
    {
        if (_communityHubButton is null)
        {
            return;
        }

        _communityHubButton.Content = string.Equals(
            _settings.Language,
            "es",
            StringComparison.OrdinalIgnoreCase)
            ? "🏆 Comunidad"
            : "🏆 Community";
    }

    private void OpenCommunityHub_Click(object sender, RoutedEventArgs e)
    {
        if (!_languageSelectionReady)
        {
            return;
        }

        try
        {
            LauncherCommunityHubWindow.Show(this, _settings);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void MainWindow_CommunityHubClosed(object? sender, EventArgs e)
    {
        Closed -= MainWindow_CommunityHubClosed;
        LanguageComboBox.SelectionChanged -= CommunityHubLanguage_SelectionChanged;
        if (_communityHubButton is not null)
        {
            _communityHubButton.Click -= OpenCommunityHub_Click;
        }
    }
}
