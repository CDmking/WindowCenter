# WindowCenter

全局快捷键将当前活动窗口居中到所在显示器的工作区（避开任务栏）。

按下 **Ctrl + Alt + C**（可自定义），窗口瞬间居中。

## 特性

- **全局快捷键** — 默认 `Ctrl + Alt + C`，可通过托盘菜单自定义，任何应用内均可触发
- **多显示器支持** — 窗口在哪个屏幕就居中到哪个屏幕
- **任务栏避让** — 使用显示器工作区（`rcWork`），不会遮挡或被任务栏遮挡
- **智能还原** — 最大化的窗口先还原再居中，最小化的窗口先恢复再居中
- **系统托盘** — 安静驻留通知区域，左键单击也可居中
- **快捷键持久化** — 自定义快捷键保存在 `config.ini`，重启后自动恢复
- **开机自启** — 支持注册表或计划任务两种方式，后者以最高权限运行且不弹 UAC
- **单实例** — 防止重复启动

## 使用

| 操作 | 方式 |
|------|------|
| 居中窗口 | 快捷键（默认 `Ctrl + Alt + C`），或左键单击托盘图标 |
| 修改快捷键 | 右键托盘 → 修改快捷键 → 按下新组合 → 确定 |
| 设置自启 | 右键托盘 → 开机自启 → 选择模式 |
| 重新注册快捷键 | 右键托盘 → 重新注册快捷键（解决热键冲突） |
| 退出 | 右键托盘 → 退出 |

### 开机自启模式

| 模式 | 权限 | UAC 弹窗 | 适用场景 |
|------|------|---------|---------|
| 关闭 | — | — | 不自动启动 |
| 注册表 | 普通 | 无 | 日常使用，不居中管理员窗口 |
| 计划任务 | 最高 | 仅首次设置时弹一次 | 推荐，开机静默启动 |

## 构建

### 环境

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 / 11

### 命令

```bash
# 编译
dotnet build -c Release

# 发布为独立单文件
dotnet publish -c Release -o dist
```

产物：`dist/WindowCenter.exe`（自包含，无需安装 .NET 运行时）

## 项目结构

```
.
├── WindowCenter.csproj      # .NET 10 WinExe，独立发布
├── app.manifest             # UAC 清单（asInvoker）
├── Program.cs               # 入口 + 系统托盘 + 右键菜单
├── NativeMethods.cs         # Windows API P/Invoke 声明
├── WindowCenterer.cs        # 居中逻辑核心
├── HotkeyManager.cs         # 全局快捷键注册（支持自定义）
├── HotkeyChangeDialog.cs    # 快捷键录入对话框（低级键盘钩子捕获）
├── ConfigManager.cs         # config.ini 读写（自定义快捷键持久化）
├── AutoStartManager.cs      # 开机自启（注册表 / 计划任务）
└── Logger.cs                # 文件日志（log.txt）

```

## 技术要点

| API | 用途 |
|-----|------|
| `RegisterHotKey` | 注册全局热键（默认 `Ctrl+Alt+C`，可自定义） |
| `GetForegroundWindow` | 获取当前活动窗口句柄 |
| `GetWindowRect` | 获取窗口位置和尺寸 |
| `MonitorFromWindow` + `GetMonitorInfo` | 获取窗口所在显示器的工作区 |
| `SetWindowPos` | 移动窗口到居中位置 |
| `Shell_NotifyIcon` | 系统托盘图标 |
| `SetWindowsHookExW` | 低级键盘钩子，用于快捷键录入对话框捕获按键 |
| `schtasks.exe` | 计划任务方式开机自启 |
| `Registry.CurrentUser\...\Run` | 注册表方式开机自启 |

## 配置文件

用户修改快捷键后，程序在同目录创建 `config.ini`：

```ini
[Hotkey]
Modifiers=6
Key=67
```

`Modifiers` 为修饰键位掩码（Ctrl=2, Alt=1, Shift=4, Win=8），`Key` 为虚拟键码（VK）。未修改过快捷键时该文件不存在，程序使用默认值。

## 日志

运行时日志写入 `log.txt`。

```
══════════ 启动 xxxx-xx-xx xx:xx:xx ══════════
[INFO]  快捷键 Ctrl+Alt+C 注册成功
[INFO]  窗口居中: 1200x800 → (360, 200)
[INFO]  快捷键已更改为 Ctrl+Shift+V
[INFO]  开机自启切换: None → ScheduledTask
[INFO]  用户退出
```
