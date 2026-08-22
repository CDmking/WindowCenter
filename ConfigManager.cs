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

namespace WindowCenter;

/// <summary>INI 配置文件管理（仅用户主动设置时才创建）</summary>
internal static class ConfigManager
{
    private const string FileName = "config.ini";

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, FileName);

    /// <summary>读取快捷键配置。返回 false 表示文件不存在或读取失败</summary>
    public static bool TryReadHotkey(out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        try
        {
            if (!File.Exists(FilePath)) return false;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Modifiers="))
                    uint.TryParse(trimmed["Modifiers=".Length..], out modifiers);
                else if (trimmed.StartsWith("Key="))
                    uint.TryParse(trimmed["Key=".Length..], out vk);
            }

            return modifiers != 0 && vk != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>保存快捷键配置（创建或覆盖 config.ini）</summary>
    public static void SaveHotkey(uint modifiers, uint vk)
    {
        var lines = new[]
        {
            "[Hotkey]",
            $"Modifiers={modifiers}",
            $"Key={vk}",
        };
        File.WriteAllLines(FilePath, lines);
    }
}
