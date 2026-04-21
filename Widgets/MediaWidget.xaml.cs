using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using Windows.Media.Control;

namespace LegendBar.Widgets
{
    public sealed partial class MediaWidget : UserControl
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;

        private DispatcherQueueTimer? _volumeFadeTimer;

        private void ShowVolume(float volume)
        {
            VolumeText.Text = $"{(int)(volume * 100)}%";
            VolumeIndicator.Visibility = Visibility.Visible;
            VolumeIndicator.Opacity = 1;

            // Reset fade timer
            if (_volumeFadeTimer == null)
            {
                _volumeFadeTimer = DispatcherQueue.CreateTimer();
                _volumeFadeTimer.IsRepeating = false;
                _volumeFadeTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _volumeFadeTimer.Tick += (s, e) =>
                {
                    VolumeIndicator.Opacity = 0;
                    VolumeIndicator.Visibility = Visibility.Collapsed;
                };
            }

            _volumeFadeTimer.Stop();
            _volumeFadeTimer.Start();
        }

        // ── Volume helper ──────────────────────────────────────────────────

        private NAudio.CoreAudioApi.AudioSessionControl? GetAppAudioSession()
        {
            try
            {
                if (_currentSession == null) return null;

                var deviceEnumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var device = deviceEnumerator.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);

                var sessions = device.AudioSessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    try
                    {
                        // Skip if session volume is 0 — unlikely to be our player
                        if (session.SimpleAudioVolume.Volume == 0) continue;

                        var process = Process.GetProcessById((int)session.GetProcessID);
                        string procName = process.ProcessName.ToLowerInvariant();

                        // Skip system processes
                        if (procName == "idle" || procName == "system" ||
                            procName == "svchost" || procName == "legendbar") continue;

                        // Match against SMTC session source app
                        string sourceId = _currentSession?.SourceAppUserModelId?.ToLowerInvariant() ?? "";

                        // Try direct name match first
                        if (sourceId.Contains(procName) || procName.Contains(sourceId))
                            return session;

                        // Fallback — return first non-system audio session
                        // that matches known media app patterns
                        if (procName.Contains("firefox") || procName.Contains("chrome") ||
                            procName.Contains("msedge") || procName.Contains("spotify") ||
                            procName.Contains("vlc") || procName.Contains("screenbox") ||
                            procName.Contains("wmplayer") || procName.Contains("groove") ||
                            procName.Contains("zune") || procName.Contains("foobar") ||
                            procName.Contains("aimp") || procName.Contains("musicbee") ||
                            procName.Contains("potplayer") || procName.Contains("mpc"))
                            return session;
                    }
                    catch { continue; }
                }

                System.Diagnostics.Debug.WriteLine("[Media] No matching session found");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Media] Exception: {ex.Message}");
            }
            return null;
        }

        // ── Constructor ────────────────────────────────────────────────────

        public MediaWidget()
        {
            this.InitializeComponent();
            InitMediaAsync();
            this.PointerWheelChanged += MediaWidget_PointerWheelChanged;
        }

        // ── Scroll to change app volume ────────────────────────────────────

        private void MediaWidget_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            var session = GetAppAudioSession();
            if (session == null) return;

            float change = delta > 0 ? 0.01f : -0.01f;
            float newVol = Math.Clamp(session.SimpleAudioVolume.Volume + change, 0f, 1f);
            session.SimpleAudioVolume.Volume = newVol;

            ShowVolume(newVol);
            e.Handled = true;
        }

        // ── SMTC ───────────────────────────────────────────────────────────

        private async void InitMediaAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.SessionsChanged += Manager_SessionsChanged;
            UpdateCurrentSession();
        }

        private void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender,
            SessionsChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(UpdateCurrentSession);
        }

        private void UpdateCurrentSession()
        {
            if (_manager == null) return;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
            }

            _currentSession = _manager.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                UpdateMediaInfo();
                UpdatePlaybackState();

                // Fade in
                var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(500))
                };
                var storyboardIn = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, this);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");
                storyboardIn.Children.Add(fadeIn);
                storyboardIn.Begin();
            }
            else
            {
                TitleText.Text = "No media";
                ArtistText.Text = "";
                DotSeparator.Visibility = Visibility.Collapsed;

                // Fade out
                var fadeOut = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(500))
                };
                var storyboardOut = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeOut, this);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeOut, "Opacity");
                storyboardOut.Children.Add(fadeOut);
                storyboardOut.Begin();
            }
        }

        private async void UpdateMediaInfo()
        {
            if (_currentSession == null) return;
            try
            {
                var props = await _currentSession.TryGetMediaPropertiesAsync();
                DispatcherQueue.TryEnqueue(() =>
                {
                    TitleText.Text = string.IsNullOrEmpty(props.Title) ? "Unknown" : props.Title;
                    ArtistText.Text = string.IsNullOrEmpty(props.Artist) ? "" : props.Artist;
                    DotSeparator.Visibility = string.IsNullOrEmpty(props.Artist)
                        ? Visibility.Collapsed : Visibility.Visible;
                });
            }
            catch { }
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender,
            MediaPropertiesChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(UpdateMediaInfo);
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender,
            PlaybackInfoChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(UpdatePlaybackState);
        }

        private void UpdatePlaybackState()
        {
            if (_currentSession == null) return;
            var playback = _currentSession.GetPlaybackInfo();
            bool isPlaying = playback.PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            PlayIcon.Visibility = isPlaying ? Visibility.Collapsed : Visibility.Visible;
            PauseIcon.Visibility = isPlaying ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            var playback = _currentSession.GetPlaybackInfo();
            if (playback.PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                await _currentSession.TryPauseAsync();
            else
                await _currentSession.TryPlayAsync();
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            await _currentSession.TrySkipNextAsync();
        }

        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            await _currentSession.TrySkipPreviousAsync();
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;

            try
            {
                string sourceId = _currentSession.SourceAppUserModelId ?? "";
                System.Diagnostics.Debug.WriteLine($"[Media] TitleClick sourceId: {sourceId}");

                // For UWP/packaged apps (contains '!') — use ApplicationFrameHost
                if (sourceId.Contains("!"))
                {
                    // Extract package name for matching window title
                    var frameHosts = Process.GetProcessesByName("ApplicationFrameHost");
                    foreach (var p in frameHosts)
                    {
                        if (p.MainWindowHandle != IntPtr.Zero)
                        {
                            ShowWindow(p.MainWindowHandle, 9);
                            SetForegroundWindow(p.MainWindowHandle);
                            return;
                        }
                    }
                }

                // For classic apps — use NAudio session PID
                var session = GetAppAudioSession();
                if (session == null) return;

                uint pid = session.GetProcessID;
                var process = Process.GetProcessById((int)pid);
                var processes = Process.GetProcessesByName(process.ProcessName);
                foreach (var p in processes)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(p.MainWindowHandle, 9);
                        SetForegroundWindow(p.MainWindowHandle);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Media] TitleClick exception: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    }
}