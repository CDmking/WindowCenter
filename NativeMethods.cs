// WindowCenter — 快捷键居中 Windows 窗口
// Copyright (C) 2026  WindowCenter contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;

namespace WindowCenter;

/// <summary>Windows API P/Invoke 声明</summary>
internal static partial class NativeMethods
{
    // ─── 常量 ───────────────────────────────────────

    public const int MOD_ALT      = 0x0001;
    public const int MOD_CONTROL  = 0x0002;
    public const int MOD_NOREPEAT = 0x4000;

    public const int WM_HOTKEY    = 0x0312;
    public const int WM_USER      = 0x0400;

    public const uint SWP_NOSIZE     = 0x0001;
    public const uint SWP_NOZORDER   = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const int SW_RESTORE   = 9;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_MAXIMIZE  = 3;

    public const uint MONITOR_DEFAULTTONEAREST = 2;

    public const int GWL_STYLE   = -16;
    public const long WS_MAXIMIZE = 0x01000000;
    public const long WS_MINIMIZE = 0x20000000;

    // ─── 结构体 ─────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width  => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int    cbSize;
        public RECT   rcMonitor;  // 显示器完整区域
        public RECT   rcWork;     // 工作区域（排除任务栏）
        public uint   dwFlags;
    }

    // ─── user32.dll ────────────────────────────────

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(nint hWnd, int id);

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    public static partial nint MonitorFromWindow(nint hWnd, uint dwFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(nint hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    public static partial long GetWindowLongPtrW(nint hWnd, int nIndex);

    // ─── kernel32.dll ─────────────────────────────

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GetConsoleWindow();
}
