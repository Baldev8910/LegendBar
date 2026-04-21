using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LegendBar.Helpers
{
    /// <summary>
    /// Detects all monitors at startup and exposes layout constants
    /// so nothing in the codebase needs to be hardcoded.
    /// </summary>
    public static class MonitorHelper
    {
        // ── Physical pixel values (for MoveAndResize / RectInt32) ──────────
        public static int WinX { get; private set; }   // leftmost physical X - 8
        public static int WinW { get; private set; }   // total physical width + 16
        public static int WinY { get; private set; }   // Y offset to hide black line

        // ── Logical pixel values (for mouse detection / AppBar) ────────────
        public static int MouseXMin { get; private set; }  // leftmost logical X
        public static int MouseXMax { get; private set; }  // rightmost logical X
        public static int PrimaryOffsetX { get; private set; } // logical offset to primary monitor (for XAML margin)

        // ── Per-monitor info ───────────────────────────────────────────────
        public static List<MonitorInfo> Monitors { get; private set; } = new();
        public static MonitorInfo? Primary { get; private set; }

        public static void Initialize()
        {
            Monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

            if (Monitors.Count == 0)
            {
                // Fallback — single 1920x1080 at 100% DPI
                WinX = -8;
                WinW = 1936;
                WinY = -4;
                MouseXMin = 0;
                MouseXMax = 1920;
                PrimaryOffsetX = 0;
                return;
            }

            // Find primary monitor
            Primary = Monitors.Find(m => m.IsPrimary);
            if (Primary == null) Primary = Monitors[0];

            // Calculate virtual desktop physical bounds
            int physLeft = int.MaxValue;
            int physRight = int.MinValue;

            foreach (var m in Monitors)
            {
                if (m.PhysicalBounds.Left < physLeft)
                    physLeft = m.PhysicalBounds.Left;
                if (m.PhysicalBounds.Right > physRight)
                    physRight = m.PhysicalBounds.Right;
            }

            int totalPhysicalWidth = physRight - physLeft;

            // Window position — 8px beyond edges for border hiding
            WinX = physLeft - 8;
            WinW = totalPhysicalWidth + 16;

            // WinY — based on primary monitor DPI
            // At 100% DPI: -4 works
            // At 125% DPI: need more overlap
            float primaryDpi = Primary.DpiScale;
            WinY = primaryDpi > 1.0f ? (int)Math.Round(-4 * primaryDpi) : -4;

            // Logical bounds for mouse detection and AppBar
            int logLeft = int.MaxValue;
            int logRight = int.MinValue;

            foreach (var m in Monitors)
            {
                if (m.LogicalBounds.Left < logLeft)
                    logLeft = m.LogicalBounds.Left;
                if (m.LogicalBounds.Right > logRight)
                    logRight = m.LogicalBounds.Right;
            }

            MouseXMin = logLeft;
            MouseXMax = logRight;

            // Primary offset — how many logical pixels from the left edge
            // of the virtual desktop to the left edge of the primary monitor
            // Used for XAML margin to push widgets onto primary monitor
            PrimaryOffsetX = Primary.LogicalBounds.Left - logLeft;

            System.Diagnostics.Debug.WriteLine($"[Monitor] Detected {Monitors.Count} monitor(s)");
            System.Diagnostics.Debug.WriteLine($"[Monitor] WinX={WinX} WinW={WinW} WinY={WinY}");
            System.Diagnostics.Debug.WriteLine($"[Monitor] MouseXMin={MouseXMin} MouseXMax={MouseXMax}");
            System.Diagnostics.Debug.WriteLine($"[Monitor] PrimaryOffsetX={PrimaryOffsetX}");
            foreach (var m in Monitors)
                System.Diagnostics.Debug.WriteLine($"[Monitor] {(m.IsPrimary ? "PRIMARY" : "secondary")} " +
                    $"phys={m.PhysicalBounds} log={m.LogicalBounds} dpi={m.DpiScale:F2}");
        }

        // ── Win32 monitor enumeration ──────────────────────────────────────

        private static bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData)
        {
            var info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf(info);
            if (GetMonitorInfo(hMonitor, ref info))
            {
                // Get DPI for this monitor
                GetDpiForMonitor(hMonitor, 0, out uint dpiX, out uint dpiY);
                float scale = dpiX / 96f;

                // Physical bounds = what Windows reports in MONITORINFOEX (already physical)
                var physBounds = new Bounds(
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right,
                    info.rcMonitor.Bottom);

                // Logical bounds = physical / scale
                var logBounds = new Bounds(
                    (int)Math.Round(info.rcMonitor.Left / scale),
                    (int)Math.Round(info.rcMonitor.Top / scale),
                    (int)Math.Round(info.rcMonitor.Right / scale),
                    (int)Math.Round(info.rcMonitor.Bottom / scale));

                bool isPrimary = (info.dwFlags & 0x1) != 0;

                Monitors.Add(new MonitorInfo
                {
                    PhysicalBounds = physBounds,
                    LogicalBounds = logBounds,
                    DpiScale = scale,
                    IsPrimary = isPrimary
                });
            }
            return true;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType,
            out uint dpiX, out uint dpiY);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }
    }

    public class MonitorInfo
    {
        public Bounds PhysicalBounds { get; set; }
        public Bounds LogicalBounds { get; set; }
        public float DpiScale { get; set; }
        public bool IsPrimary { get; set; }
    }

    public struct Bounds
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public Bounds(int left, int top, int right, int bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }

        public override string ToString() =>
            $"({Left},{Top})-({Right},{Bottom}) {Width}x{Height}";
    }
}