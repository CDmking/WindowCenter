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

namespace WindowCenter;

/// <summary>简易文件日志，写入程序所在目录</summary>
internal static class Logger
{
    private static readonly string LogDir;
    private static readonly string LogFile;
    private static readonly object LockObj = new();

    static Logger()
    {
        LogDir  = AppContext.BaseDirectory;
        LogFile = Path.Combine(LogDir, "log.txt");
    }

    /// <summary>初始化（确保目录存在，写入启动分隔线）</summary>
    public static void Init()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            Write("══════════ 启动 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ══════════");
        }
        catch { /* 日志不应该影响程序正常运行 */ }
    }

    public static void Info(string message)  => Write($"[INFO]  {message}");
    public static void Warn(string message)  => Write($"[WARN]  {message}");
    public static void Error(string message) => Write($"[ERROR] {message}");

    private static void Write(string line)
    {
        try
        {
            lock (LockObj)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
        }
        catch { /* 静默失败 */ }
    }
}
