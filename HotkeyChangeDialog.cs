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
using System.Windows.Forms;
using static WindowCenter.NativeMethods;

namespace WindowCenter;

/// <summary>快捷键录入对话框：通过低级键盘钩子捕获用户按下的组合键</summary>
internal sealed class HotkeyChangeDialog : Form
{
    private readonly Label _label;
    private readonly Label _hint;
    private readonly Button _okBtn;
    private readonly Button _cancelBtn;

    private uint _capturedMods;
    private uint _capturedVK;
    private nint _hookId;
    private readonly HookProc _hookProc;

    public uint ResultModifiers => _capturedMods;
    public uint ResultKey => _capturedVK;

    public HotkeyChangeDialog()
    {
        _hookProc = HookCallback;

        Text = "设置快捷键";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(340, 190);

        _label = new Label
        {
            Text = "（等待输入…）",
            Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 18),
            Size = new Size(285, 35),
        };

        _hint = new Label
        {
            Text = "请按下新的快捷键组合\n（至少包含 Ctrl / Alt / Win 之一）",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 58),
            Size = new Size(285, 40),
        };

        _okBtn = new Button
        {
            Text = "确定",
            Enabled = false,
            DialogResult = DialogResult.OK,
            Location = new Point(140, 110),
            Size = new Size(80, 30),
        };
        _okBtn.Click += (_, _) => Close();

        _cancelBtn = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(230, 110),
            Size = new Size(80, 30),
        };
        _cancelBtn.Click += (_, _) => Close();

        Controls.AddRange([_label, _hint, _okBtn, _cancelBtn]);
        AcceptButton = _okBtn;
        CancelButton = _cancelBtn;

        Load += (_, _) =>
        {
            _hookId = SetWindowsHookExW(WH_KEYBOARD_LL, _hookProc,
                GetModuleHandleW(nint.Zero), 0);
        };
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            int vk = Marshal.ReadInt32(lParam);

            if (!IsModifierKey(vk))
            {
                var mods = GetModifierState();
                if (mods != 0)
                {
                    _capturedMods = mods;
                    _capturedVK = (uint)vk;
                    BeginInvoke(new Action(() =>
                    {
                        _label.Text = FormatHotkey(_capturedMods, _capturedVK);
                        _okBtn.Enabled = true;
                    }));
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_hookId != nint.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = nint.Zero;
        }
        base.OnFormClosed(e);
    }

    // ── 辅助方法 ─────────────────────────────────

    private static uint GetModifierState()
    {
        uint mods = 0;
        if ((Control.ModifierKeys & Keys.Control) != 0) mods |= MOD_CONTROL;
        if ((Control.ModifierKeys & Keys.Alt) != 0)      mods |= MOD_ALT;
        if ((Control.ModifierKeys & Keys.Shift) != 0)    mods |= MOD_SHIFT;
        if (HasWinKey())                                  mods |= MOD_WIN;
        return mods;
    }

    private static bool HasWinKey()
    {
        return (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0
            || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
    }

    private static bool IsModifierKey(int vk)
        => vk is VK_LCONTROL or VK_RCONTROL or VK_LMENU or VK_RMENU
            or VK_LSHIFT or VK_RSHIFT or VK_LWIN or VK_RWIN;

    public static string FormatHotkey(uint mods, uint vk)
    {
        var parts = new List<string>();
        if ((mods & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & MOD_ALT) != 0)     parts.Add("Alt");
        if ((mods & MOD_SHIFT) != 0)   parts.Add("Shift");
        if ((mods & MOD_WIN) != 0)     parts.Add("Win");
        parts.Add(GetKeyName((Keys)vk));
        return string.Join("+", parts);
    }

    private static string GetKeyName(Keys key)
    {
        return key switch
        {
            >= Keys.D0 and <= Keys.D9 => (key - Keys.D0).ToString(),
            >= Keys.A and <= Keys.Z   => key.ToString(),
            >= Keys.F1 and <= Keys.F12 => key.ToString(),
            Keys.Space     => "Space",
            Keys.Return    => "Enter",
            Keys.Escape    => "Esc",
            Keys.Back      => "Backspace",
            Keys.Tab       => "Tab",
            Keys.Delete    => "Del",
            Keys.Insert    => "Ins",
            Keys.Home      => "Home",
            Keys.End       => "End",
            Keys.PageUp    => "PgUp",
            Keys.Next      => "PgDn",
            Keys.Up        => "↑",
            Keys.Down      => "↓",
            Keys.Left      => "←",
            Keys.Right     => "→",
            Keys.OemMinus  => "-",
            Keys.Oemplus   => "=",
            Keys.OemPeriod => ".",
            Keys.Oemcomma  => ",",
            Keys.OemSemicolon => ";",
            Keys.OemQuestion  => "/",
            _              => key.ToString(),
        };
    }
}
