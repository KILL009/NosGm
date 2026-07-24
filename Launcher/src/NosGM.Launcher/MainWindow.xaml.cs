// SPDX-License-Identifier: MIT

using System.Windows;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

public partial class MainWindow : Window
{
    private readonly LauncherController _controller = new();
    private LauncherSettings _settings = new();
    private CancellationTokenSource? _operationCancellation;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await LauncherSettingsStore.LoadAsync();
            InstallRootTextBox.Text = _settings.InstallRoot;
            var recovery = await _controller.RecoverAsync(
                _settings,
                progress: null,
                CancellationToken.None);

            ChannelStatusRun.Text = TrustedChannel.IsConfigured
                ? $"configurado con clave {TrustedChannel.KeyId}"
                : "desactivado hasta fijar HTTPS, keyId y clave pública";
            var recoveredCount = recovery.RecoveredTransactions +
                                 recovery.FinalizedTransactions +
                                 recovery.DiscardedTransactions;
            if (recoveredCount > 0)
            {
                StatusTextBlock.Text = "Recuperación automática completada";
                DetailTextBlock.Text =
                    $"Se procesaron {recoveredCount} transacciones pendientes sin tocar archivos ajenos.";
            }
            else
            {
                StatusTextBlock.Text = TrustedChannel.IsConfigured
                    ? "Listo para comprobar actualizaciones"
                    : "Base segura instalada";
                DetailTextBlock.Text = TrustedChannel.IsConfigured
                    ? "El launcher verificará la firma del manifiesto antes de leer cualquier ruta de actualización."
                    : "La interfaz no descargará nada mientras TrustedChannel.cs conserve los valores de ejemplo.";
            }

            SetButtonsEnabled(true);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
        => await RunUpdateAsync(apply: false, "Comprobando instalación...");

    private async void Repair_Click(object sender, RoutedEventArgs e)
        => await RunUpdateAsync(apply: true, "Verificando y reparando...");

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show(
            this,
            "La importación registrará únicamente archivos existentes cuyas rutas aparezcan en el manifiesto firmado. " +
            "Los archivos distintos podrán ser reemplazados posteriormente al pulsar Reparar. Los archivos extra no se tocarán.\n\n¿Continuar?",
            "Importar instalación existente",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice == MessageBoxResult.Yes)
        {
            await RunImportAsync();
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LauncherController.LaunchGame(_settings);
            StatusTextBlock.Text = "Juego iniciado";
            DetailTextBlock.Text = "NosGM inició el cliente sin solicitar elevación administrativa.";
            if (_settings.CloseAfterLaunch)
            {
                Close();
            }
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LauncherController.OpenInstallFolder(_settings);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private async Task RunImportAsync()
    {
        BeginOperation("Analizando instalación existente...");
        var progress = new Progress<UpdateProgress>(UpdateProgress);
        try
        {
            var result = await _controller.ImportExistingAsync(
                _settings,
                progress,
                _operationCancellation!.Token);
            StatusTextBlock.Text = "Instalación importada";
            DetailTextBlock.Text =
                $"{result.ManagedFiles} archivos administrados: {result.MatchingFiles} correctos, " +
                $"{result.RepairFiles} para reparar y {result.MissingFiles} ausentes.";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Operación cancelada";
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task RunUpdateAsync(bool apply, string status)
    {
        BeginOperation(status);
        var progress = new Progress<UpdateProgress>(UpdateProgress);
        try
        {
            var operation = await _controller.CheckAndApplyAsync(
                _settings,
                apply,
                progress,
                _operationCancellation!.Token);

            if (operation.Plan.Downloads.Count == 0 && operation.Plan.Deletes.Count == 0)
            {
                StatusTextBlock.Text = "La instalación está actualizada";
                DetailTextBlock.Text = operation.Plan.IgnoredDeletes.Count == 0
                    ? "Todos los archivos firmados coinciden con el manifiesto."
                    : $"Se ignoraron {operation.Plan.IgnoredDeletes.Count} eliminaciones no administradas.";
            }
            else if (!apply)
            {
                StatusTextBlock.Text = "Actualización disponible";
                DetailTextBlock.Text =
                    $"{operation.Plan.Downloads.Count} archivos, {operation.Plan.DownloadBytes:N0} bytes y " +
                    $"{operation.Plan.Deletes.Count} eliminaciones administradas.";
            }
            else
            {
                StatusTextBlock.Text = "Actualización completada";
                DetailTextBlock.Text =
                    $"Versión {operation.Result?.ReleaseId}; " +
                    $"{operation.Result?.DownloadedFiles ?? 0} archivos instalados.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Operación cancelada";
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void BeginOperation(string status)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = new CancellationTokenSource();
        SetButtonsEnabled(false);
        StatusTextBlock.Text = status;
        DetailTextBlock.Text = string.Empty;
        UpdateProgressBar.Value = 0;
    }

    private void UpdateProgress(UpdateProgress update)
    {
        var percent = update.TotalBytes > 0
            ? Math.Clamp(update.CompletedBytes * 100d / update.TotalBytes, 0d, 100d)
            : update.TotalFiles > 0
                ? Math.Clamp(update.CompletedFiles * 100d / update.TotalFiles, 0d, 100d)
                : 0d;

        UpdateProgressBar.Value = percent;
        ProgressTextBlock.Text = $"{percent:0.0} %";
        if (!string.IsNullOrWhiteSpace(update.Path))
        {
            DetailTextBlock.Text = $"{update.Phase}: {update.Path}";
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        ImportButton.IsEnabled = enabled && TrustedChannel.IsConfigured;
        CheckButton.IsEnabled = enabled && TrustedChannel.IsConfigured;
        RepairButton.IsEnabled = enabled && TrustedChannel.IsConfigured;
        PlayButton.IsEnabled = enabled;
    }

    private void ShowError(Exception exception)
    {
        StatusTextBlock.Text = "No se pudo completar la operación";
        DetailTextBlock.Text = exception.Message;
        MessageBox.Show(this, exception.Message, "NosGM Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
    }
}
