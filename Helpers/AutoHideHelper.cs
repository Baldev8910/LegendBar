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

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelMouseProc? _mouseProc;
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
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                bool mouseAtTopEdge = hookStruct.pt.Y <= 2
                    && hookStruct.pt.X >= MouseXMin
                    && hookStruct.pt.X <= MouseXMax;

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

        // Hidden Y = just above screen so 1px sliver remains visible
        private int HiddenY() => (int)-(_barHeight - 1) - 8;

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

        private void CheckTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            GetCursorPos(out POINT pos);

            bool mouseInsideBar = pos.Y <= _logicalBarHeight
                && pos.X >= MouseXMin
                && pos.X <= MouseXMax;

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

        private void AnimateTo(int targetY, int durationMs)
        {
            var startY = _appWindow.Position.Y;
            var startTime = DateTime.Now;

            var animTimer = _dispatcherQueue.CreateTimer();
            animTimer.Interval = TimeSpan.FromMilliseconds(10);
            animTimer.Tick += (s, e) =>
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
                    s.Stop();
            };
            animTimer.Start();
        }
    }
}