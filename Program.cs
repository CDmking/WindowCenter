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

using System.ComponentModel;
using System.Windows.Forms;

namespace WindowCenter;

internal sealed class MainForm : Form
{
    private readonly HotkeyManager _hotkey;
    private readonly NotifyIcon    _trayIcon;
    private readonly Container     _components;

    // 需要动态更新文本的菜单项
    private readonly ToolStripMenuItem _centerItem;
    private readonly ToolStripMenuItem _reRegisterItem;

    // 开机自启菜单项（用于动态更新显示文本）
    private readonly ToolStripMenuItem _autoStartMenu;
    private readonly ToolStripMenuItem _autoStartOff;
    private readonly ToolStripMenuItem _autoStartRegistry;
    private readonly ToolStripMenuItem _autoStartTask;

    public MainForm()
    {
        _components = new Container();
        _hotkey     = new HotkeyManager(this);

        // 读取配置文件中的自定义快捷键
        if (ConfigManager.TryReadHotkey(out var mods, out var vk))
        {
            _hotkey.SetHotkey(mods, vk);
            Logger.Info($"从配置文件加载快捷键: {HotkeyChangeDialog.FormatHotkey(mods, vk)}");
        }

        // 构建菜单并持有引用
        (_trayIcon, _centerItem, _reRegisterItem, _autoStartMenu, _autoStartOff, _autoStartRegistry, _autoStartTask)
            = CreateTrayIcon();

        // 隐藏主窗口
        ShowInTaskbar = false;
        WindowState   = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
        Size = new Size(0, 0);

        // 注册快捷键
        if (_hotkey.Register())
            Logger.Info("快捷键 Ctrl+Alt+C 注册成功");
        else
            Logger.Warn("快捷键注册失败，可能被其他程序占用");

        Load += (_, _) =>
        {
            Hide();
            NativeMethods.ShowWindow(Handle, 0);
        };
    }

    // ── 托盘图标 + 菜单 ──────────────────────────

    private (NotifyIcon, ToolStripMenuItem, ToolStripMenuItem, ToolStripMenuItem,
             ToolStripMenuItem, ToolStripMenuItem, ToolStripMenuItem)
        CreateTrayIcon()
    {
        // 图标：蓝色 16x16 十字准星
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.DodgerBlue);
        using var pen = new Pen(Color.White, 2);
        g.DrawLine(pen, 8, 3, 8, 13);
        g.DrawLine(pen, 3, 8, 13, 8);
        var icon = Icon.FromHandle(bmp.GetHicon());

        // ── 构建菜单 ──────────────────────────────

        var hotkeyText = HotkeyChangeDialog.FormatHotkey(_hotkey.CurrentModifiers, _hotkey.CurrentKey);

        // 居中
        var centerItem = new ToolStripMenuItem($"🖥  居中当前窗口 ({hotkeyText})")
        { Font = new Font(SystemFonts.MenuFont!, FontStyle.Bold) };
        centerItem.Click += (_, _) => WindowCenterer.CenterActiveWindow();

        // 自启父菜单
        var autoStartMenu = new ToolStripMenuItem("📌 开机自启");

        var offItem      = new ToolStripMenuItem("○ 关闭");
        var registryItem = new ToolStripMenuItem("📝 注册表（普通权限）");
        var taskItem     = new ToolStripMenuItem("⏱  计划任务（管理员权限，无 UAC 弹窗）");

        offItem.Click      += OnAutoStartChanged;
        registryItem.Click += OnAutoStartChanged;
        taskItem.Click     += OnAutoStartChanged;

        autoStartMenu.DropDownItems.AddRange([
            offItem, registryItem, taskItem,
        ]);

        // 刷新菜单勾选状态
        RefreshAutoStartChecks(offItem, registryItem, taskItem, autoStartMenu);

        // 修改快捷键
        var changeHotkeyItem = new ToolStripMenuItem("⌨  修改快捷键");
        changeHotkeyItem.Click += (_, _) => ChangeHotkey();

        // 重新注册
        var reRegisterItem = new ToolStripMenuItem("🔄 重新注册快捷键");
        reRegisterItem.Click += (_, _) =>
        {
            _hotkey.Unregister();
            if (_hotkey.Register())
            {
                Logger.Info("快捷键重新注册成功");
                _trayIcon.ShowBalloonTip(1500, "WindowCenter", "✅ 快捷键已重新注册。", ToolTipIcon.Info);
            }
            else
            {
                Logger.Warn("快捷键重新注册失败");
                _trayIcon.ShowBalloonTip(3000, "WindowCenter", "⚠ 注册失败，请检查是否被其他程序占用。", ToolTipIcon.Warning);
            }
        };

        // 退出
        var exitItem = new ToolStripMenuItem("❌ 退出");
        exitItem.Click += (_, _) =>
        {
            Logger.Info("用户退出");
            _trayIcon.Visible = false;
            Application.Exit();
        };

        var menu = new ContextMenuStrip(_components);
        menu.Items.AddRange([
            centerItem,
            new ToolStripSeparator(),
            autoStartMenu,
            new ToolStripSeparator(),
            changeHotkeyItem,
            reRegisterItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        // ── 托盘图标 ──────────────────────────────
        var notifyIcon = new NotifyIcon(_components)
        {
            Icon             = icon,
            Text             = $"WindowCenter - {hotkeyText} 居中窗口",
            Visible          = true,
            ContextMenuStrip = menu,
        };

        notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                WindowCenterer.CenterActiveWindow();
        };

        return (notifyIcon, centerItem, reRegisterItem, autoStartMenu, offItem, registryItem, taskItem);
    }

    // ── 自启菜单事件 ─────────────────────────────

    private void OnAutoStartChanged(object? sender, EventArgs e)
    {
        var clicked = (ToolStripMenuItem)sender!;
        AutoStartMode mode;

        if (clicked == _autoStartOff)
            mode = AutoStartMode.None;
        else if (clicked == _autoStartRegistry)
            mode = AutoStartMode.Registry;
        else
            mode = AutoStartMode.ScheduledTask;

        try
        {
            AutoStartManager.SetMode(mode);
        }
        catch (UnauthorizedAccessException)
        {
            _trayIcon.ShowBalloonTip(3000, "WindowCenter",
                "⚠ 已取消。创建计划任务需要管理员授权。",
                ToolTipIcon.Warning);
        }

        RefreshAutoStartChecks(_autoStartOff, _autoStartRegistry, _autoStartTask, _autoStartMenu);
    }

    private static void RefreshAutoStartChecks(
        ToolStripMenuItem off, ToolStripMenuItem registry, ToolStripMenuItem task,
        ToolStripMenuItem parent)
    {
        var mode = AutoStartManager.CurrentMode;

        off.Checked      = mode == AutoStartMode.None;
        registry.Checked = mode == AutoStartMode.Registry;
        task.Checked     = mode == AutoStartMode.ScheduledTask;

        parent.Text = mode switch
        {
            AutoStartMode.None          => "📌 开机自启：关闭",
            AutoStartMode.Registry      => "📌 开机自启：注册表",
            AutoStartMode.ScheduledTask => "📌 开机自启：计划任务",
            _                           => "📌 开机自启",
        };
    }

    // ── 快捷键修改 ───────────────────────────────

    private void UpdateHotkeyDisplay()
    {
        var text = HotkeyChangeDialog.FormatHotkey(_hotkey.CurrentModifiers, _hotkey.CurrentKey);
        _centerItem.Text = $"🖥  居中当前窗口 ({text})";
        _trayIcon.Text = $"WindowCenter - {text} 居中窗口";
    }

    private void ChangeHotkey()
    {
        using var dialog = new HotkeyChangeDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        uint mods = dialog.ResultModifiers;
        uint vk = dialog.ResultKey;

        _hotkey.Unregister();
        _hotkey.SetHotkey(mods, vk);

        if (_hotkey.Register())
        {
            ConfigManager.SaveHotkey(mods, vk);
            Logger.Info($"快捷键已更改为 {HotkeyChangeDialog.FormatHotkey(mods, vk)}");
            _trayIcon.ShowBalloonTip(1500, "WindowCenter",
                $"✅ 快捷键已更改为 {HotkeyChangeDialog.FormatHotkey(mods, vk)}",
                ToolTipIcon.Info);
        }
        else
        {
            Logger.Warn("新快捷键注册失败，可能被其他程序占用");
            _trayIcon.ShowBalloonTip(3000, "WindowCenter",
                "⚠ 新快捷键注册失败，请检查是否被其他程序占用。",
                ToolTipIcon.Warning);
        }

        UpdateHotkeyDisplay();
    }

    // ── 消息处理 ──────────────────────────────────

    protected override void WndProc(ref Message m)
    {
        if (!_hotkey.HandleMessage(ref m))
            base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkey.Dispose();
            _components.Dispose();
        }
        base.Dispose(disposing);
    }
}

// ── 入口 ──────────────────────────────────────────

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Logger.Init();
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, "WindowCenter_SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("WindowCenter 已经在运行中。\n请查看系统托盘。",
                "WindowCenter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
        GC.KeepAlive(mutex);
    }
}
