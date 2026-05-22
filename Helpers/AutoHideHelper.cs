using LegendBar.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using AppWindow = Microsoft.UI.Windowing.AppWindow;

namespace LegendBar.Helpers
{
    public class AutoHideHelper
    {
        private DispatcherQueueTimer? _animTimer;
        private static LowLevelMouseProc? _mouseProc;

        // All layout values come from MonitorHelper — nothing hardcoded
        private int WindowX => MonitorHelper.WinX;
        private int WindowW => MonitorHelper.WinW;
        private int ShownY => MonitorHelper.WinY;
        private int MouseXMin => MonitorHelper.MouseXMin;
        private int MouseXMax => MonitorHelper.MouseXMax;

        private bool _isPinnedByUser = false;
        private readonly AppWindow _appWindow;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _checkTimer;
        private readonly DispatcherQueueTimer _hideDelayTimer;
        private bool _isVisible;
        private bool _isPinned;
        private bool _externalWindowOpen = false;
        private double _barHeight = 50;
        private double _logicalBarHeight = 50;
        private int _showDurationMs = 150;
        private int _hideDurationMs = 200;
        private const int HideDelayMs = 300;
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private float _dpiScale = 1.0f;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static IntPtr _hookID = IntPtr.Zero;

        public void SetPinnedPosition(bool pinned) { } // no-op

        public void SetPinned(bool pinned)
        {
            _isPinnedByUser = pinned;
            if (pinned)
            {
                _checkTimer.Stop();
                _hideDelayTimer.Stop();
                _isVisible = true;
            }
            else
            {
                _checkTimer.Start();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;

        private void InstallMouseHook()
        {
            _mouseProc = MouseHookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                GetModuleHandle(curModule.ModuleName!), 0);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEMOVE)
            {
                try
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    // Hook receives physical pixel coordinates
                    // Scale logical X bounds to physical for correct comparison
                    int topEdgeThreshold = (int)Math.Round(2 * _dpiScale);
                    int physMouseXMin = (int)Math.Round(MouseXMin * _dpiScale);
                    int physMouseXMax = (int)Math.Round(MouseXMax * _dpiScale);
                    bool mouseAtTopEdge = hookStruct.pt.Y <= topEdgeThreshold
                        && hookStruct.pt.X >= physMouseXMin
                        && hookStruct.pt.X <= physMouseXMax;

                    if (mouseAtTopEdge && !_isVisible)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            _hideDelayTimer.Stop();
                            _isVisible = true;
                            AnimateTo(ShownY, _showDurationMs);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AutoHide] Hook callback exception: {ex}");
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        public AutoHideHelper(AppWindow appWindow, DispatcherQueue dispatcherQueue,
            double initialHeight = 50)
        {
            _appWindow = appWindow;
            _dispatcherQueue = dispatcherQueue;
            _isVisible = false;
            _isPinned = false;
            _logicalBarHeight = initialHeight;
            _barHeight = initialHeight;
            _dpiScale = MonitorHelper.PrimaryDpiScale;

            // Start hidden
            int hiddenY = HiddenY();
            _appWindow.MoveAndResize(
                new RectInt32(WindowX, hiddenY, WindowW, (int)_barHeight));

            _checkTimer = dispatcherQueue.CreateTimer();
            _checkTimer.Interval = TimeSpan.FromMilliseconds(16);
            _checkTimer.Tick += CheckTimer_Tick;
            _checkTimer.Start();

            InstallMouseHook();

            _hideDelayTimer = dispatcherQueue.CreateTimer();
            _hideDelayTimer.Interval = TimeSpan.FromMilliseconds(HideDelayMs);
            _hideDelayTimer.IsRepeating = false;
            _hideDelayTimer.Tick += HideDelayTimer_Tick;
        }

        // Hidden Y — in physical pixels, must push bar fully off screen
        private int HiddenY() => (int)Math.Round(-(_barHeight * _dpiScale) - 8);

        public void Dispose()
        {
            _checkTimer.Stop();
            _hideDelayTimer.Stop();
            _animTimer?.Stop();
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        public void UpdateBarHeight(double height)
        {
            _logicalBarHeight = height;
            _barHeight = height;
        }

        public void UpdateSpeeds(double showMs, double hideMs)
        {
            _showDurationMs = (int)showMs;
            _hideDurationMs = (int)hideMs;
        }

        public void UpdateHideDelay(int delayMs)
        {
            _hideDelayTimer.Interval = TimeSpan.FromMilliseconds(delayMs);
        }

        public void SetExternalWindowOpen(bool isOpen)
        {
            _externalWindowOpen = isOpen;
            if (isOpen) _hideDelayTimer.Stop();
        }

        public void SetFullScreenMode(bool isFullScreen)
        {
            if (isFullScreen)
            {
                _checkTimer.Stop();
                _hideDelayTimer.Stop();
                _animTimer?.Stop();
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }
            }
            else
            {
                if (_hookID == IntPtr.Zero)
                    InstallMouseHook();
                if (!_isPinnedByUser)
                    _checkTimer.Start();
            }
        }

        private void CheckTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            GetCursorPos(out POINT pos);

            // GetCursorPos returns physical pixel coordinates
            // Scale everything to physical for correct comparison
            var primary = MonitorHelper.Primary;
            int physXMin = primary?.PhysicalBounds.Left ?? MouseXMin;
            int physXMax = primary?.PhysicalBounds.Right ?? MouseXMax;
            double physBarHeight = _barHeight * _dpiScale;

            bool mouseInsideBar = pos.Y <= physBarHeight
                && pos.X >= physXMin
                && pos.X <= physXMax;

            bool leftClicked = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
            bool rightClicked = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

            if ((leftClicked || rightClicked) && mouseInsideBar)
            {
                _isPinned = true;
                _hideDelayTimer.Stop();
            }

            if (!mouseInsideBar && _isPinned)
                _isPinned = false;

            if (!mouseInsideBar && _isVisible && !_isPinned && !_externalWindowOpen)
            {
                if (!_hideDelayTimer.IsRunning)
                    _hideDelayTimer.Start();
            }
            else if ((mouseInsideBar || _isPinned) && _isVisible)
            {
                _hideDelayTimer.Stop();
            }
        }

        public void ForceHide()
        {
            _isVisible = false;
            _isPinned = false;
            _hideDelayTimer.Stop();
            _appWindow.MoveAndResize(
                new RectInt32(WindowX, HiddenY(), WindowW, (int)_barHeight));
        }

        private void HideDelayTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            _isVisible = false;
            AnimateTo(HiddenY(), _hideDurationMs);
        }

        public void ForceShow()
        {
            _isVisible = true;
            _hideDelayTimer.Stop();
            AnimateTo(ShownY, _showDurationMs);
        }

        private void AnimateTo(int targetY, int durationMs)
        {
            _animTimer?.Stop();
            var startY = _appWindow.Position.Y;
            var startTime = DateTime.Now;

            _animTimer = _dispatcherQueue.CreateTimer();
            _animTimer.Interval = TimeSpan.FromMilliseconds(10);
            _animTimer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                double progress = Math.Min(elapsed / durationMs, 1.0);

                double eased = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;

                int currentY = (int)(startY + (targetY - startY) * eased);
                _appWindow.MoveAndResize(
                    new RectInt32(WindowX, currentY, WindowW, (int)_barHeight));

                if (progress >= 1.0)
                    _animTimer.Stop();
            };
            _animTimer.Start();
        }
    }
}