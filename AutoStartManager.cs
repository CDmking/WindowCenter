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

using System.Diagnostics;
using Microsoft.Win32;

namespace WindowCenter;

/// <summary>开机自启模式</summary>
public enum AutoStartMode
{
    None,
    Registry,       // HKCU\Run — 不需要管理员权限
    ScheduledTask   // 计划任务 — 最高权限且不弹UAC
}

/// <summary>开机自启管理（注册表 / 计划任务）</summary>
internal static class AutoStartManager
{
    private const string RunKey    = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowCenter";
    private const string TaskName  = "WindowCenter";

    // ── 当前模式 ──────────────────────────────────

    public static AutoStartMode CurrentMode
    {
        get
        {
            if (IsScheduledTaskEnabled()) return AutoStartMode.ScheduledTask;
            if (IsRegistryEnabled())      return AutoStartMode.Registry;
            return AutoStartMode.None;
        }
    }

    // ── 设置模式 ──────────────────────────────────

    /// <summary>切换到指定模式（自动清除其他模式）</summary>
    public static void SetMode(AutoStartMode mode)
    {
        // 先全部清除
        DisableRegistry();
        DisableScheduledTask();

        // 再启用目标
        Logger.Info($"开机自启切换: {CurrentMode} → {mode}");

        switch (mode)
        {
            case AutoStartMode.Registry:
                EnableRegistry();
                break;
            case AutoStartMode.ScheduledTask:
                EnableScheduledTask();
                break;
        }
    }

    // ═══════════════════════════════════════════════
    //  注册表
    // ═══════════════════════════════════════════════

    private static bool IsRegistryEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is not null;
    }

    private static void EnableRegistry()
    {
        string exePath = Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(ValueName, $"\"{exePath}\"");
    }

    private static void DisableRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    // ═══════════════════════════════════════════════
    //  计划任务
    // ═══════════════════════════════════════════════

    private static bool IsScheduledTaskEnabled()
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "schtasks.exe",
            Arguments              = $"/query /tn \"{TaskName}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void EnableScheduledTask()
    {
        string exePath = Application.ExecutablePath;

        var psi = new ProcessStartInfo
        {
            FileName        = "schtasks.exe",
            Arguments       = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl HIGHEST /f",
            UseShellExecute = true,
            Verb            = "runas",  // 创建计划任务需要管理员权限
            CreateNoWindow  = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception)
        {
            // 用户取消了 UAC 弹窗
            throw new UnauthorizedAccessException("需要管理员权限来创建计划任务。");
        }
    }

    private static void DisableScheduledTask()
    {
        var psi = new ProcessStartInfo
        {
            FileName        = "schtasks.exe",
            Arguments       = $"/delete /tn \"{TaskName}\" /f",
            UseShellExecute = true,
            Verb            = "runas",
            CreateNoWindow  = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception)
        {
            // 用户取消了 UAC 弹窗，忽略
        }
    }
}
