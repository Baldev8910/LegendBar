using LegendBar.Helpers;
using System;
using System.Runtime.InteropServices;

namespace LegendBar.Helpers
{
    public static class AppBarHelper
    {
        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_QUERYPOS = 0x00000002;
        private const int ABM_SETPOS = 0x00000003;
        private const int ABE_TOP = 1;
        private const uint WS_POPUP = 0x80000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(
            uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(
            uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        private static bool _isRegistered = false;
        private static readonly System.Collections.Generic.List<IntPtr> _secondaryHwnds = new();

        public static void Register(IntPtr hwnd, int barHeight)
        {
            if (_isRegistered) return;

            // Register AppBar for each monitor separately
            foreach (var monitor in MonitorHelper.Monitors)
            {
                // Convert physical bounds to logical for AppBar API
                int logLeft = monitor.LogicalBounds.Left;
                int logRight = monitor.LogicalBounds.Right;
                int logHeight = (int)Math.Round(barHeight / monitor.DpiScale);

                if (monitor.IsPrimary)
                {
                    // Use the real window handle for the primary monitor
                    RegisterSingleAppBar(hwnd, logLeft, logRight, logHeight);
                }
                else
                {
                    // Create a dummy window for secondary monitors
                    var secondaryHwnd = CreateWindowEx(
                        0, "Static", "LegendBarSecondary",
                        WS_POPUP,
                        monitor.PhysicalBounds.Left, 0,
                        monitor.PhysicalBounds.Width, barHeight,
                        IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    if (secondaryHwnd != IntPtr.Zero)
                    {
                        _secondaryHwnds.Add(secondaryHwnd);
                        RegisterSingleAppBar(secondaryHwnd, logLeft, logRight, logHeight);
                    }
                }
            }

            SystemParametersInfo(0x002F, 0, IntPtr.Zero, 0x0002);
            _isRegistered = true;
        }

        private static void RegisterSingleAppBar(IntPtr hwnd, int left, int right, int height)
        {
            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hwnd;

            SHAppBarMessage(ABM_NEW, ref abd);

            abd.uEdge = ABE_TOP;
            abd.rc.left = left;
            abd.rc.top = 0;
            abd.rc.right = right;
            abd.rc.bottom = height;

            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            abd.rc.top = 0;
            abd.rc.bottom = height;
            SHAppBarMessage(ABM_SETPOS, ref abd);
        }

        public static void Unregister(IntPtr hwnd)
        {
            if (!_isRegistered) return;

            // Unregister primary
            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = hwnd;
            SHAppBarMessage(ABM_REMOVE, ref abd);

            // Unregister and destroy all secondary dummy windows
            foreach (var secondaryHwnd in _secondaryHwnds)
            {
                var abd2 = new APPBARDATA();
                abd2.cbSize = Marshal.SizeOf(abd2);
                abd2.hWnd = secondaryHwnd;
                SHAppBarMessage(ABM_REMOVE, ref abd2);
                DestroyWindow(secondaryHwnd);
            }
            _secondaryHwnds.Clear();

            SystemParametersInfo(0x002F, 0, IntPtr.Zero, 0x0002);
            _isRegistered = false;
        }

        public static bool IsRegistered => _isRegistered;
    }
}