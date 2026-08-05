// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NosGM.Launcher;

internal static class LauncherOperationStatusLayoutModule
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
            window.EnsureOperationStatusLayout();
        }
    }
}

public partial class MainWindow
{
    private const double OperationStatusMinimumHeight = 150;

    internal void EnsureOperationStatusLayout()
    {
        if (StatusTextBlock.Parent is not StackPanel summaryPanel)
        {
            return;
        }

        var operationBorder = FindVisualAncestor<Border>(summaryPanel);
        if (operationBorder is null
            || VisualTreeHelper.GetParent(operationBorder) is not Grid dashboardGrid)
        {
            return;
        }

        var rowIndex = Grid.GetRow(operationBorder);
        if (rowIndex < 0 || rowIndex >= dashboardGrid.RowDefinitions.Count)
        {
            return;
        }

        var operationRow = dashboardGrid.RowDefinitions[rowIndex];
        operationRow.Height = GridLength.Auto;
        operationRow.MinHeight = Math.Max(
            operationRow.MinHeight,
            OperationStatusMinimumHeight);

        operationBorder.MinHeight = Math.Max(
            operationBorder.MinHeight,
            OperationStatusMinimumHeight);
        operationBorder.VerticalAlignment = VerticalAlignment.Stretch;
        summaryPanel.VerticalAlignment = VerticalAlignment.Center;
    }

    private static T? FindVisualAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        DependencyObject? current = child;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
