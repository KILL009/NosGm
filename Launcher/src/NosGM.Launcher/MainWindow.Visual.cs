// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NosGM.Launcher;

public partial class MainWindow
{
    private static readonly string[] SupportedMusicExtensions = [".mp3", ".wav", ".wma"];
    private readonly MediaPlayer _musicPlayer = new();
    private readonly List<string> _musicTracks = [];
    private readonly DispatcherTimer _musicTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _serverStatusTimer = new() { Interval = TimeSpan.FromSeconds(20) };
    private CancellationTokenSource? _serverProbeCancellation;
    private bool _visualFeaturesInitialized;
    private bool _musicIsPlaying;
    private bool _updatingMusicProgress;
    private int _musicTrackIndex = -1;

    private string MusicFolderPath => Path.Combine(AppContext.BaseDirectory, "Music");

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_visualFeaturesInitialized)
        {
            return;
        }

        _visualFeaturesInitialized = true;
        _musicPlayer.Volume = MusicVolumeSlider.Value;
        _musicPlayer.MediaOpened += MusicPlayer_MediaOpened;
        _musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
        _musicPlayer.MediaFailed += MusicPlayer_MediaFailed;
        _musicTimer.Tick += MusicTimer_Tick;
        _serverStatusTimer.Tick += ServerStatusTimer_Tick;
        Closed += MainWindow_VisualClosed;

        InitializeMusicPlaylist();
        _musicTimer.Start();
        _serverStatusTimer.Start();
        _ = RefreshServerStatusAsync();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
        => Close();

    private async void RefreshServerStatus_Click(object sender, RoutedEventArgs e)
        => await RefreshServerStatusAsync();

    private async void ServerStatusTimer_Tick(object? sender, EventArgs e)
        => await RefreshServerStatusAsync();

    private async Task RefreshServerStatusAsync()
    {
        _serverProbeCancellation?.Cancel();
        _serverProbeCancellation?.Dispose();
        _serverProbeCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var token = _serverProbeCancellation.Token;

        SetServerChecking(MasterStatusDot, MasterStatusText);
        SetServerChecking(WorldStatusDot, WorldStatusText);
        SetServerChecking(LoginStatusDot, LoginStatusText);
        ServerProbeTimeTextBlock.Text = "Comprobando servicios...";

        try
        {
            var host = string.IsNullOrWhiteSpace(_settings.LoginServerAddress)
                ? "127.0.0.1"
                : _settings.LoginServerAddress;

            var masterTask = CanConnectAsync(host, 4545, token);
            var worldTask = CanConnectAsync(host, 1337, token);
            var loginTask = CanConnectAsync(host, 4005, token);
            await Task.WhenAll(masterTask, worldTask, loginTask);

            SetServerState(MasterStatusDot, MasterStatusText, await masterTask);
            SetServerState(WorldStatusDot, WorldStatusText, await worldTask);
            SetServerState(LoginStatusDot, LoginStatusText, await loginTask);
            ServerProbeTimeTextBlock.Text = $"Última comprobación: {DateTime.Now:HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            ServerProbeTimeTextBlock.Text = "Comprobación cancelada";
        }
        catch (Exception exception)
        {
            SetServerState(MasterStatusDot, MasterStatusText, false);
            SetServerState(WorldStatusDot, WorldStatusText, false);
            SetServerState(LoginStatusDot, LoginStatusText, false);
            ServerProbeTimeTextBlock.Text = $"Estado no disponible: {exception.Message}";
        }
    }

    private static async Task<bool> CanConnectAsync(string host, int port, CancellationToken token)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(1500));
            using var client = new TcpClient(AddressFamily.InterNetwork);
            await client.ConnectAsync(host, port, timeout.Token);
            return client.Connected;
        }
        catch (Exception exception) when (
            exception is SocketException or OperationCanceledException or IOException)
        {
            return false;
        }
    }

    private static void SetServerChecking(Ellipse dot, TextBlock label)
    {
        dot.Fill = FrozenBrush(255, 184, 77);
        label.Foreground = FrozenBrush(168, 180, 204);
        label.Text = "Comprobando";
    }

    private static void SetServerState(Ellipse dot, TextBlock label, bool online)
    {
        dot.Fill = online
            ? FrozenBrush(62, 232, 143)
            : FrozenBrush(255, 93, 122);
        label.Foreground = dot.Fill;
        label.Text = online ? "En línea" : "Fuera de línea";
    }

    private static SolidColorBrush FrozenBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private void InitializeMusicPlaylist()
    {
        Directory.CreateDirectory(MusicFolderPath);
        _musicTracks.Clear();
        _musicTracks.AddRange(
            Directory.EnumerateFiles(MusicFolderPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path => SupportedMusicExtensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase))
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase));

        if (_musicTracks.Count == 0)
        {
            _musicTrackIndex = -1;
            MusicTrackTextBlock.Text = "Sin canciones";
            MusicDetailTextBlock.Text = "Pulsa ♫ y añade MP3, WAV o WMA";
            MusicPlayPauseButton.Content = "▶";
            MusicProgressSlider.Value = 0;
            MusicProgressSlider.Maximum = 1;
            MusicCurrentTimeTextBlock.Text = "00:00";
            MusicDurationTextBlock.Text = "00:00";
            return;
        }

        LoadMusicTrack(0, playImmediately: false);
    }

    private void LoadMusicTrack(int index, bool playImmediately)
    {
        if (_musicTracks.Count == 0)
        {
            InitializeMusicPlaylist();
            return;
        }

        _musicTrackIndex = (index + _musicTracks.Count) % _musicTracks.Count;
        var path = _musicTracks[_musicTrackIndex];
        _musicPlayer.Close();
        _musicPlayer.Open(new Uri(path, UriKind.Absolute));
        MusicTrackTextBlock.Text = Path.GetFileNameWithoutExtension(path);
        MusicDetailTextBlock.Text = $"Pista {_musicTrackIndex + 1} de {_musicTracks.Count}";
        MusicProgressSlider.Value = 0;
        MusicCurrentTimeTextBlock.Text = "00:00";
        MusicDurationTextBlock.Text = "00:00";

        if (playImmediately)
        {
            _musicPlayer.Play();
            _musicIsPlaying = true;
            MusicPlayPauseButton.Content = "Ⅱ";
        }
        else
        {
            _musicIsPlaying = false;
            MusicPlayPauseButton.Content = "▶";
        }
    }

    private void MusicPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_musicTracks.Count == 0)
        {
            InitializeMusicPlaylist();
            if (_musicTracks.Count == 0)
            {
                OpenMusicFolder();
                return;
            }
        }

        if (_musicIsPlaying)
        {
            _musicPlayer.Pause();
            _musicIsPlaying = false;
            MusicPlayPauseButton.Content = "▶";
        }
        else
        {
            if (_musicTrackIndex < 0)
            {
                LoadMusicTrack(0, playImmediately: false);
            }
            _musicPlayer.Play();
            _musicIsPlaying = true;
            MusicPlayPauseButton.Content = "Ⅱ";
        }
    }

    private void PreviousTrack_Click(object sender, RoutedEventArgs e)
        => LoadMusicTrack(_musicTrackIndex <= 0 ? _musicTracks.Count - 1 : _musicTrackIndex - 1, _musicIsPlaying);

    private void NextTrack_Click(object sender, RoutedEventArgs e)
        => LoadMusicTrack(_musicTrackIndex + 1, _musicIsPlaying);

    private void MusicPlayer_MediaOpened(object? sender, EventArgs e)
    {
        if (_musicPlayer.NaturalDuration.HasTimeSpan)
        {
            MusicProgressSlider.Maximum = Math.Max(1, _musicPlayer.NaturalDuration.TimeSpan.TotalSeconds);
            MusicDurationTextBlock.Text = FormatTime(_musicPlayer.NaturalDuration.TimeSpan);
        }
    }

    private void MusicPlayer_MediaEnded(object? sender, EventArgs e)
        => LoadMusicTrack(_musicTrackIndex + 1, playImmediately: true);

    private void MusicPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
    {
        _musicIsPlaying = false;
        MusicPlayPauseButton.Content = "▶";
        MusicDetailTextBlock.Text = $"No se pudo reproducir: {e.ErrorException.Message}";
    }

    private void MusicTimer_Tick(object? sender, EventArgs e)
    {
        if (_updatingMusicProgress)
        {
            return;
        }

        _updatingMusicProgress = true;
        try
        {
            var position = _musicPlayer.Position;
            MusicCurrentTimeTextBlock.Text = FormatTime(position);
            if (_musicPlayer.NaturalDuration.HasTimeSpan)
            {
                MusicProgressSlider.Maximum = Math.Max(1, _musicPlayer.NaturalDuration.TimeSpan.TotalSeconds);
                MusicProgressSlider.Value = Math.Clamp(
                    position.TotalSeconds,
                    MusicProgressSlider.Minimum,
                    MusicProgressSlider.Maximum);
            }
        }
        finally
        {
            _updatingMusicProgress = false;
        }
    }

    private void MusicProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_musicTrackIndex >= 0 && !_updatingMusicProgress)
        {
            _musicPlayer.Position = TimeSpan.FromSeconds(MusicProgressSlider.Value);
        }
    }

    private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => _musicPlayer.Volume = Math.Clamp(e.NewValue, 0, 1);

    private void OpenMusicFolder_Click(object sender, RoutedEventArgs e)
        => OpenMusicFolder();

    private void OpenMusicFolder()
    {
        Directory.CreateDirectory(MusicFolderPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = MusicFolderPath,
            UseShellExecute = true
        });
    }

    private void OpenNews_Click(object sender, MouseButtonEventArgs e)
        => OpenUrl("https://github.com/KILL009/NosGm/commits/main");

    private void OpenExternalLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string url || string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show(
                this,
                "Este enlace comunitario todavía no está configurado.",
                "NosGM Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        OpenUrl(url);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static string FormatTime(TimeSpan value)
        => $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";

    private void MainWindow_VisualClosed(object? sender, EventArgs e)
    {
        _musicTimer.Stop();
        _serverStatusTimer.Stop();
        _musicPlayer.Stop();
        _musicPlayer.Close();
        _serverProbeCancellation?.Cancel();
        _serverProbeCancellation?.Dispose();
    }
}
