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

using System.Windows.Forms;
using static WindowCenter.NativeMethods;

namespace WindowCenter;

/// <summary>全局快捷键管理（默认 Ctrl + Alt + C）</summary>
internal sealed class HotkeyManager : IDisposable
{
    private const int HOTKEY_ID = 9001;

    private readonly Control _owner;
    private bool _registered;
    private uint _modifiers = MOD_CONTROL | MOD_ALT;
    private uint _vk = (uint)Keys.C;

    public uint CurrentModifiers => _modifiers;
    public uint CurrentKey => _vk;

    public HotkeyManager(Control owner)
    {
        _owner = owner;
    }

    /// <summary>设置快捷键（不会自动注册，需手动调用 Register）</summary>
    public void SetHotkey(uint modifiers, uint vk)
    {
        _modifiers = modifiers | MOD_NOREPEAT;
        _vk = vk;
    }

    /// <summary>注册全局快捷键</summary>
    public bool Register()
    {
        if (_registered) return true;
        _registered = RegisterHotKey(_owner.Handle, HOTKEY_ID, _modifiers, _vk);
        return _registered;
    }

    /// <summary>注销全局快捷键</summary>
    public bool Unregister()
    {
        if (!_registered) return true;

        _registered = !UnregisterHotKey(_owner.Handle, HOTKEY_ID);
        return !_registered;
    }

    /// <summary>处理 WndProc 消息。返回 true 表示已处理。</summary>
    public bool HandleMessage(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam == HOTKEY_ID)
        {
            WindowCenterer.CenterActiveWindow();
            return true;
        }
        return false;
    }

    public void Dispose() => Unregister();
}
