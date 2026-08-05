// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace NosGM.Launcher;

internal sealed class LauncherDiagnosticsWindow : Window
{
    private readonly LauncherSettings _settings;
    private readonly LauncherDiagnosticsService _service = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TextBlock _summaryText = new();
    private readonly TextBlock _detailText = new();
    private readonly ProgressBar _progress = new();
    private readonly StackPanel _checksPanel = new();
    private readonly Button _runButton;
    private readonly Button _exportButton;

    private LauncherDiagnosticReport? _report;
    private bool _running;

    public LauncherDiagnosticsWindow(LauncherSettings settings)
    {
        _settings = settings;
        Title = "Centro de diagnóstico de NosGM";
        Width = 900;
        Height = 680;
        MinWidth = 720;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Background = FindBrush("WindowBackgroundBrush", Color.FromRgb(7, 11, 24));
        Foreground = FindBrush("TextPrimaryBrush", Colors.White);

        _runButton = CreateButton("Ejecutar diagnóstico", RunButton_Click);
        _exportButton = CreateButton("Exportar ZIP para soporte", ExportButton_Click);
        _exportButton.IsEnabled = false;

        Content = BuildLayout();
        Loaded += DiagnosticsWindow_Loaded;
        Closed += DiagnosticsWindow_Closed;
    }

    private UIElement BuildLayout()
    {
        var root = new Grid
        {
            Margin = new Thickness(22)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titlePanel = new StackPanel();
        titlePanel.Children.Add(new TextBlock
        {
            Text = "DIAGNÓSTICO DEL LAUNCHER",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AccentBlueBrush", Color.FromRgb(56, 189, 248))
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Detecta fallos antes de entrar al juego",
            FontSize = 27,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 5, 0, 0)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Revisa el cliente, permisos, disco, portal, autenticación y servicios de NosGM sin leer contraseñas ni tickets.",
            FontSize = 12,
            Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(168, 180, 204)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 18, 0)
        });
        header.Children.Add(titlePanel);

        var closeButton = CreateButton("Cerrar", (_, _) => Close());
        closeButton.MinWidth = 86;
        Grid.SetColumn(closeButton, 1);
        header.Children.Add(closeButton);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var summaryCard = CreateCard();
        summaryCard.Margin = new Thickness(0, 18, 0, 14);
        var summaryGrid = new Grid();
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

        var summaryPanel = new StackPanel();
        _summaryText.Text = "Preparando diagnóstico...";
        _summaryText.FontSize = 18;
        _summaryText.FontWeight = FontWeights.SemiBold;
        summaryPanel.Children.Add(_summaryText);
        _detailText.Text = "La comprobación tarda pocos segundos.";
        _detailText.Margin = new Thickness(0, 5, 0, 0);
        _detailText.FontSize = 11;
        _detailText.Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(168, 180, 204));
        _detailText.TextWrapping = TextWrapping.Wrap;
        summaryPanel.Children.Add(_detailText);
        summaryGrid.Children.Add(summaryPanel);

        var progressPanel = new StackPanel
        {
            Margin = new Thickness(24, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Height = 8;
        _progress.Value = 0;
        progressPanel.Children.Add(_progress);
        var privacyText = new TextBlock
        {
            Text = "Privacidad: datos sensibles excluidos",
            HorizontalAlignment = HorizontalAlignment.Right,
            FontSize = 10,
            Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(168, 180, 204)),
            Margin = new Thickness(0, 7, 0, 0)
        };
        progressPanel.Children.Add(privacyText);
        Grid.SetColumn(progressPanel, 1);
        summaryGrid.Children.Add(progressPanel);
        summaryCard.Child = summaryGrid;
        Grid.SetRow(summaryCard, 1);
        root.Children.Add(summaryCard);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _checksPanel.Margin = new Thickness(0, 0, 6, 0);
        scroll.Content = _checksPanel;
        Grid.SetRow(scroll, 2);
        root.Children.Add(scroll);

        var footer = new Grid
        {
            Margin = new Thickness(0, 16, 0, 0)
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        footer.Children.Add(new TextBlock
        {
            Text = "El ZIP contiene un informe, un resumen seguro y la huella SHA-256 del cliente. No contiene la cuenta.",
            FontSize = 10,
            Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(168, 180, 204)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 18, 0)
        });
        Grid.SetColumn(_runButton, 1);
        footer.Children.Add(_runButton);
        Grid.SetColumn(_exportButton, 2);
        footer.Children.Add(_exportButton);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        return root;
    }

    private async void DiagnosticsWindow_Loaded(object sender, RoutedEventArgs e)
        => await RunDiagnosticsAsync();

    private async void RunButton_Click(object sender, RoutedEventArgs e)
        => await RunDiagnosticsAsync();

    private async Task RunDiagnosticsAsync()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _runButton.IsEnabled = false;
        _exportButton.IsEnabled = false;
        _checksPanel.Children.Clear();
        _summaryText.Text = "Comprobando NosGM...";
        _summaryText.Foreground = Foreground;
        _detailText.Text = "Inspeccionando instalación, red y servicios.";
        _progress.IsIndeterminate = true;

        try
        {
            _report = await _service.RunAsync(_settings, _lifetime.Token);
            RenderReport(_report);
            _exportButton.IsEnabled = true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Closing the window cancels the current run.
        }
        catch (Exception exception)
        {
            _summaryText.Text = "El diagnóstico no pudo completarse";
            _summaryText.Foreground = FindBrush("DangerBrush", Color.FromRgb(255, 93, 122));
            _detailText.Text = exception.Message;
        }
        finally
        {
            _progress.IsIndeterminate = false;
            _progress.Value = _report is null ? 0 : 100;
            _runButton.IsEnabled = !_lifetime.IsCancellationRequested;
            _running = false;
        }
    }

    private void RenderReport(LauncherDiagnosticReport report)
    {
        var passed = report.Checks.Count(check => check.Status == LauncherDiagnosticStatus.Passed);
        var warnings = report.Checks.Count(check => check.Status == LauncherDiagnosticStatus.Warning);
        var failed = report.Checks.Count(check => check.Status == LauncherDiagnosticStatus.Failed);

        _summaryText.Text = report.OverallStatus switch
        {
            LauncherDiagnosticStatus.Failed => "NosGM necesita atención",
            LauncherDiagnosticStatus.Warning => "NosGM funciona con advertencias",
            _ => "NosGM está listo para jugar"
        };
        _summaryText.Foreground = StatusBrush(report.OverallStatus);
        _detailText.Text = $"Correctos: {passed}  •  Advertencias: {warnings}  •  Fallos: {failed}";

        foreach (var check in report.Checks)
        {
            _checksPanel.Children.Add(CreateCheckCard(check));
        }
    }

    private Border CreateCheckCard(LauncherDiagnosticCheck check)
    {
        var card = CreateCard();
        card.Margin = new Thickness(0, 0, 0, 9);
        card.Padding = new Thickness(14, 12, 14, 12);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new TextBlock
        {
            Text = StatusIcon(check.Status),
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = StatusBrush(check.Status),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        grid.Children.Add(icon);

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = check.Title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold
        });
        content.Children.Add(new TextBlock
        {
            Text = check.Summary,
            FontSize = 11,
            Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(168, 180, 204)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        if (!string.IsNullOrWhiteSpace(check.SuggestedAction))
        {
            content.Children.Add(new TextBlock
            {
                Text = "Acción: " + check.SuggestedAction,
                FontSize = 10,
                Foreground = StatusBrush(check.Status),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0)
            });
        }
        if (!string.IsNullOrWhiteSpace(check.Details))
        {
            content.Children.Add(new TextBlock
            {
                Text = check.Details,
                FontSize = 9,
                Foreground = FindBrush("TextSecondaryBrush", Color.FromRgb(113, 128, 156)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        Grid.SetColumn(content, 1);
        grid.Children.Add(content);
        card.Child = grid;
        return card;
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_report is null || _running)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Guardar diagnóstico de NosGM",
            Filter = "Archivo ZIP (*.zip)|*.zip",
            AddExtension = true,
            DefaultExt = ".zip",
            FileName = $"NosGM-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _exportButton.IsEnabled = false;
        _runButton.IsEnabled = false;
        _summaryText.Text = "Creando paquete para soporte...";
        _progress.IsIndeterminate = true;
        try
        {
            await _service.ExportSupportBundleAsync(
                _report,
                _settings,
                dialog.FileName,
                _lifetime.Token);
            _summaryText.Text = "Paquete de soporte creado";
            _summaryText.Foreground = FindBrush("SuccessBrush", Color.FromRgb(62, 232, 143));
            _detailText.Text = dialog.FileName;

            var choice = MessageBox.Show(
                this,
                "El ZIP fue creado sin contraseñas, tickets ni nombre de cuenta. ¿Abrir la carpeta?",
                "NosGM Diagnostics",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (choice == MessageBoxResult.Yes)
            {
                var directory = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Closing the window cancels export.
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "No se pudo exportar el diagnóstico",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _progress.IsIndeterminate = false;
            _progress.Value = 100;
            _exportButton.IsEnabled = _report is not null && !_lifetime.IsCancellationRequested;
            _runButton.IsEnabled = !_lifetime.IsCancellationRequested;
        }
    }

    private Button CreateButton(string text, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(4),
            MinWidth = 118,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        if (TryFindResource("NeonButton") is Style style)
        {
            button.Style = style;
        }
        button.Click += handler;
        return button;
    }

    private Border CreateCard()
    {
        var card = new Border
        {
            Background = FindBrush("PanelBrush", Color.FromArgb(204, 17, 24, 43)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(68, 56, 189, 248)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18)
        };
        if (TryFindResource("GlassCard") is Style style)
        {
            card.Style = style;
        }
        return card;
    }

    private Brush StatusBrush(LauncherDiagnosticStatus status)
        => status switch
        {
            LauncherDiagnosticStatus.Passed => FindBrush("SuccessBrush", Color.FromRgb(62, 232, 143)),
            LauncherDiagnosticStatus.Warning => FindBrush("WarningBrush", Color.FromRgb(255, 184, 77)),
            LauncherDiagnosticStatus.Failed => FindBrush("DangerBrush", Color.FromRgb(255, 93, 122)),
            _ => FindBrush("AccentBlueBrush", Color.FromRgb(56, 189, 248))
        };

    private static string StatusIcon(LauncherDiagnosticStatus status)
        => status switch
        {
            LauncherDiagnosticStatus.Passed => "✓",
            LauncherDiagnosticStatus.Warning => "!",
            LauncherDiagnosticStatus.Failed => "✕",
            _ => "i"
        };

    private Brush FindBrush(string key, Color fallback)
    {
        if (TryFindResource(key) is Brush brush)
        {
            return brush;
        }

        var created = new SolidColorBrush(fallback);
        created.Freeze();
        return created;
    }

    private void DiagnosticsWindow_Closed(object? sender, EventArgs e)
    {
        Loaded -= DiagnosticsWindow_Loaded;
        Closed -= DiagnosticsWindow_Closed;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _service.Dispose();
    }
}
