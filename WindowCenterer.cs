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
using static WindowCenter.NativeMethods;

namespace WindowCenter;

/// <summary>核心：将当前前台窗口居中到所在显示器</summary>
internal static class WindowCenterer
{
    /// <summary>居中当前活动窗口。成功返回 true。</summary>
    public static bool CenterActiveWindow()
    {
        nint hWnd = GetForegroundWindow();
        if (hWnd == nint.Zero) return false;

        // 跳过控制台窗口自身
        if (hWnd == GetConsoleWindow()) return false;

        // ── 1. 处理窗口状态 ──────────────────────
        long style = GetWindowLongPtrW(hWnd, GWL_STYLE);
        bool isMaximized = (style & WS_MAXIMIZE) != 0;
        bool isMinimized = (style & WS_MINIMIZE) != 0;

        // 最小化 → 先还原
        if (isMinimized)
        {
            ShowWindow(hWnd, SW_RESTORE);
            // 给窗口一点时间恢复，否则 GetWindowRect 可能拿到旧尺寸
            Thread.Sleep(50);
        }

        // 最大化 → 先还原
        if (isMaximized)
        {
            ShowWindow(hWnd, SW_RESTORE);
            Thread.Sleep(50);
        }

        // ── 2. 获取窗口尺寸 ──────────────────────
        if (!GetWindowRect(hWnd, out RECT windowRect))
            return false;

        int windowWidth  = windowRect.Width;
        int windowHeight = windowRect.Height;

        // ── 3. 获取所在显示器工作区 ──────────────
        nint hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfoW(hMonitor, ref monitorInfo))
            return false;

        RECT work = monitorInfo.rcWork;

        // ── 4. 计算居中坐标 ──────────────────────
        int newX = work.Left + (work.Width  - windowWidth)  / 2;
        int newY = work.Top  + (work.Height - windowHeight) / 2;

        // ── 5. 移动窗口（保持宽高不变，不改变 Z 序）─
        bool result = SetWindowPos(hWnd, nint.Zero,
            newX, newY, 0, 0,
            SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);

        if (result)
            Logger.Info($"窗口居中: {windowWidth}x{windowHeight} → ({newX}, {newY})");
        else
            Logger.Warn("SetWindowPos 失败");

        return result;
    }
}
