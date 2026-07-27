// SPDX-License-Identifier: MIT

using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

public partial class MainWindow : Window
{
    private readonly LauncherController _controller = new();
    private LauncherSettings _settings = new();
    private InstallFolderInspection? _folderInspection;
    private CancellationTokenSource? _operationCancellation;
    private bool _languageSelectionReady;

    public MainWindow()
    {
        LauncherText.ValidateCatalogs();
        InstallFolderText.ValidateCatalogs();
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await LauncherSettingsStore.LoadAsync();
            LanguageComboBox.ItemsSource = LauncherText.Languages;
            LanguageComboBox.SelectedValue = _settings.Language;
            _languageSelectionReady = true;
            ApplyLanguage();
            InstallRootTextBox.Text = _settings.InstallRoot;

            var recovery = await _controller.RecoverAsync(
                _settings,
                progress: null,
                CancellationToken.None);
            _folderInspection = await InstallFolderInspector.InspectAsync(
                _settings.InstallRoot,
                _settings.GameExecutable);

            var recoveredCount = recovery.RecoveredTransactions +
                                 recovery.FinalizedTransactions +
                                 recovery.DiscardedTransactions;
            if (recoveredCount > 0)
            {
                StatusTextBlock.Text = T(LauncherTextKeys.RecoveryCompleted);
                DetailTextBlock.Text = F(LauncherTextKeys.RecoveryDetail, recoveredCount);
            }
            else
            {
                ShowFolderStatus();
            }

            SetButtonsEnabled(true);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private async void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_languageSelectionReady || LanguageComboBox.SelectedValue is not string language)
        {
            return;
        }

        try
        {
            _settings = _settings with { Language = language };
            await LauncherSettingsStore.SaveAsync(_settings);
            ApplyLanguage();
            if (_operationCancellation is null)
            {
                if (_folderInspection is null)
                {
                    SetIdleStatus();
                }
                else
                {
                    ShowFolderStatus();
                }
            }
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = FT(InstallFolderTextKeys.SelectTitle),
            Multiselect = false,
            InitialDirectory = Directory.Exists(_settings.InstallRoot)
                ? _settings.InstallRoot
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        BeginOperation(T(LauncherTextKeys.Checking));
        try
        {
            var candidate = _settings with { InstallRoot = Path.GetFullPath(dialog.FolderName) };
            _ = await InstallFolderInspector.InspectAsync(
                candidate.InstallRoot,
                candidate.GameExecutable,
                _operationCancellation!.Token);
            _ = await _controller.RecoverAsync(
                candidate,
                progress: null,
                _operationCancellation.Token);
            var inspection = await InstallFolderInspector.InspectAsync(
                candidate.InstallRoot,
                candidate.GameExecutable,
                _operationCancellation.Token);

            await LauncherSettingsStore.SaveAsync(candidate);
            _settings = candidate;
            _folderInspection = inspection;
            InstallRootTextBox.Text = candidate.InstallRoot;
            ShowFolderStatus();
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = T(LauncherTextKeys.Cancelled);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private async void Check_Click(object sender, RoutedEventArgs e)
        => await RunUpdateAsync(apply: false, T(LauncherTextKeys.Checking));

    private async void Repair_Click(object sender, RoutedEventArgs e)
        => await RunUpdateAsync(apply: true, T(LauncherTextKeys.Repairing));

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show(
            this,
            T(LauncherTextKeys.ImportMessage),
            T(LauncherTextKeys.ImportTitle),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice == MessageBoxResult.Yes)
        {
            await RunImportAsync();
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        LauncherCredentials? credentials = null;
        var usesModernLogin = !string.IsNullOrWhiteSpace(_settings.AuthenticationEndpoint);
        if (usesModernLogin)
        {
            credentials = LauncherLoginDialog.Prompt(
                this,
                _settings.Language,
                _settings.AccountName);
            if (credentials is null)
            {
                return;
            }

            BeginOperation(LauncherLoginDialog.Authenticating(_settings.Language));
        }

        try
        {
            if (credentials is null)
            {
                LauncherController.LaunchGame(_settings);
                DetailTextBlock.Text = T(LauncherTextKeys.GameStartedDetail);
            }
            else
            {
                _ = await ModernGameLauncher.LaunchAsync(
                    _settings,
                    credentials.AccountName,
                    credentials.Password,
                    _operationCancellation!.Token);
                _settings = _settings with { AccountName = credentials.AccountName };
                await LauncherSettingsStore.SaveAsync(_settings);
                DetailTextBlock.Text = LauncherLoginDialog.StartedDetail(_settings.Language);
            }

            StatusTextBlock.Text = T(LauncherTextKeys.GameStarted);
            if (_settings.CloseAfterLaunch)
            {
                Close();
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = T(LauncherTextKeys.Cancelled);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            if (usesModernLogin)
            {
                EndOperation();
            }
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
        BeginOperation(T(LauncherTextKeys.Analyzing));
        var progress = new Progress<UpdateProgress>(UpdateProgress);
        try
        {
            var result = await _controller.ImportExistingAsync(
                _settings,
                progress,
                _operationCancellation!.Token);
            _folderInspection = await InstallFolderInspector.InspectAsync(
                _settings.InstallRoot,
                _settings.GameExecutable,
                _operationCancellation.Token);
            StatusTextBlock.Text = T(LauncherTextKeys.Imported);
            DetailTextBlock.Text = F(
                LauncherTextKeys.ImportDetail,
                result.ManagedFiles,
                result.MatchingFiles,
                result.RepairFiles,
                result.MissingFiles);
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = T(LauncherTextKeys.Cancelled);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            EndOperation();
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
                StatusTextBlock.Text = T(LauncherTextKeys.UpToDate);
                DetailTextBlock.Text = operation.Plan.IgnoredDeletes.Count == 0
                    ? T(LauncherTextKeys.AllFilesMatch)
                    : F(LauncherTextKeys.IgnoredDeletes, operation.Plan.IgnoredDeletes.Count);
            }
            else if (!apply)
            {
                StatusTextBlock.Text = T(LauncherTextKeys.UpdateAvailable);
                DetailTextBlock.Text = F(
                    LauncherTextKeys.UpdateAvailableDetail,
                    operation.Plan.Downloads.Count,
                    operation.Plan.DownloadBytes,
                    operation.Plan.Deletes.Count);
            }
            else
            {
                _folderInspection = await InstallFolderInspector.InspectAsync(
                    _settings.InstallRoot,
                    _settings.GameExecutable,
                    _operationCancellation.Token);
                StatusTextBlock.Text = T(LauncherTextKeys.UpdateCompleted);
                DetailTextBlock.Text = F(
                    LauncherTextKeys.UpdateCompletedDetail,
                    operation.Result?.ReleaseId ?? operation.Plan.Manifest.ReleaseId,
                    operation.Result?.DownloadedFiles ?? 0);
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = T(LauncherTextKeys.Cancelled);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            EndOperation();
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

    private void EndOperation()
    {
        _operationCancellation?.Dispose();
        _operationCancellation = null;
        SetButtonsEnabled(true);
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
            DetailTextBlock.Text = $"{LauncherText.Phase(_settings.Language, update.Phase)}: {update.Path}";
        }
    }

    private void ApplyLanguage()
    {
        SubtitleTextBlock.Text = T(LauncherTextKeys.Subtitle);
        LanguageLabelTextBlock.Text = T(LauncherTextKeys.Language);
        InstallationLabelTextBlock.Text = T(LauncherTextKeys.Installation);
        BrowseFolderButton.Content = FT(InstallFolderTextKeys.Browse);
        OpenFolderButton.Content = T(LauncherTextKeys.OpenFolder);
        ChannelLabelRun.Text = T(LauncherTextKeys.ChannelStatus);
        ImportButton.Content = T(LauncherTextKeys.Import);
        CheckButton.Content = T(LauncherTextKeys.Check);
        RepairButton.Content = T(LauncherTextKeys.Repair);
        PlayButton.Content = T(LauncherTextKeys.Play);
        ChannelStatusRun.Text = TrustedChannel.IsConfigured
            ? F(LauncherTextKeys.ChannelConfigured, TrustedChannel.KeyId)
            : T(LauncherTextKeys.ChannelDisabled);
    }

    private void ShowFolderStatus()
    {
        if (_folderInspection is null)
        {
            SetIdleStatus();
            return;
        }

        switch (_folderInspection.Kind)
        {
            case InstallFolderKind.Empty:
                StatusTextBlock.Text = FT(InstallFolderTextKeys.EmptyStatus);
                DetailTextBlock.Text = FT(InstallFolderTextKeys.EmptyDetail);
                break;
            case InstallFolderKind.ExistingClient:
                StatusTextBlock.Text = FT(InstallFolderTextKeys.ExistingStatus);
                DetailTextBlock.Text = FT(InstallFolderTextKeys.ExistingDetail);
                break;
            case InstallFolderKind.Managed:
                StatusTextBlock.Text = FT(InstallFolderTextKeys.ManagedStatus);
                DetailTextBlock.Text = FF(
                    InstallFolderTextKeys.ManagedDetail,
                    _folderInspection.ReleaseId ?? "-",
                    _folderInspection.ManagedFiles);
                break;
            default:
                throw new InvalidOperationException("Unsupported installation folder kind.");
        }
    }

    private void SetIdleStatus()
    {
        StatusTextBlock.Text = TrustedChannel.IsConfigured
            ? T(LauncherTextKeys.Ready)
            : T(LauncherTextKeys.SafeBase);
        DetailTextBlock.Text = TrustedChannel.IsConfigured
            ? T(LauncherTextKeys.ReadyDetail)
            : T(LauncherTextKeys.DisabledDetail);
    }

    private string T(string key) => LauncherText.Get(_settings.Language, key);

    private string F(string key, params object?[] arguments)
        => LauncherText.Format(_settings.Language, key, arguments);

    private string FT(string key) => InstallFolderText.Get(_settings.Language, key);

    private string FF(string key, params object?[] arguments)
        => InstallFolderText.Format(_settings.Language, key, arguments);

    private void SetButtonsEnabled(bool enabled)
    {
        BrowseFolderButton.IsEnabled = enabled;
        OpenFolderButton.IsEnabled = enabled;
        ImportButton.IsEnabled = enabled &&
                                 TrustedChannel.IsConfigured &&
                                 _folderInspection?.Kind == InstallFolderKind.ExistingClient;
        CheckButton.IsEnabled = enabled && TrustedChannel.IsConfigured && _folderInspection is not null;
        RepairButton.IsEnabled = enabled && TrustedChannel.IsConfigured && _folderInspection is not null;
        PlayButton.IsEnabled = enabled && CanLaunchGame();
        LanguageComboBox.IsEnabled = enabled;
    }

    private bool CanLaunchGame()
    {
        try
        {
            var gamePath = SafePaths.ResolveManagedPath(_settings.InstallRoot, _settings.GameExecutable);
            return File.Exists(gamePath);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void ShowError(Exception exception)
    {
        StatusTextBlock.Text = T(LauncherTextKeys.Failed);
        DetailTextBlock.Text = exception.Message;
        MessageBox.Show(this, exception.Message, "NosGM Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
    }
}
