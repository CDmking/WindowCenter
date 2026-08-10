# AGENTS.md

AI 编码代理的项目上下文。在修改或扩展此项目之前阅读。

## 项目概述

WindowCenter 是一个 Windows 桌面工具，通过全局快捷键 `Ctrl+Alt+C` 将当前活动窗口居中到所在显示器的工作区。它驻留在系统托盘中，支持开机自启。

## 技术栈

- **语言**: C# 14 (.NET 10)
- **UI 框架**: Windows Forms（仅用于托盘图标和不可见消息窗口）
- **运行时**: Windows 10/11 x64，自包含独立发布

## 构建

```bash
# Debug 构建
dotnet build

# Release 构建
dotnet build -c Release

# 独立单文件发布
dotnet publish -c Release -o dist
```

## 架构

```
Main (Program.cs)
  └─ MainForm : Form (隐藏窗口，消息泵)
       ├─ HotkeyManager      → RegisterHotKey / UnregisterHotKey，处理 WM_HOTKEY
       ├─ NotifyIcon         → 托盘图标 + 右键菜单
       │    ├─ 居中当前窗口
       │    ├─ 开机自启（关闭 / 注册表 / 计划任务）
       │    ├─ 重新注册快捷键
       │    └─ 退出
       └─ 消息循环           → 分发 WM_HOTKEY → WindowCenterer
```

## 模块职责

| 文件 | 职责 | 依赖 |
|------|------|------|
| `Program.cs` | 入口、MainForm、托盘菜单构建、消息泵 | 所有模块 |
| `NativeMethods.cs` | `static partial` 类，所有 P/Invoke + 常量 | 无 |
| `WindowCenterer.cs` | 纯静态方法 `CenterActiveWindow()` | NativeMethods, Logger |
| `HotkeyManager.cs` | `IDisposable`，包装热键注册/注销/WndProc 分发 | NativeMethods, WindowCenterer |
| `AutoStartManager.cs` | 纯静态类，注册表 + 计划任务 CRUD | `System.Diagnostics`, `Microsoft.Win32` |
| `Logger.cs` | 纯静态类，线程安全文件日志 | `System.IO` |
| `app.manifest` | UAC `asInvoker` + Win10 兼容性 | — |

## 编码约定

- **P/Invoke**: 所有 Windows API 声明集中在 `NativeMethods.cs`，使用 `LibraryImportAttribute`（源码生成），因此项目需要 `<AllowUnsafeBlocks>true`
- **Nullable**: 启用 `<Nullable>enable</Nullable>`，所有引用类型默认为不可空，需要时显式标记 `?`
- **日志**: 使用 `Logger.Info/Warn/Error`，日志写入程序所在目录的 `windowcenter.log`（`AppContext.BaseDirectory`）。日志失败不得抛出异常
- **权限**: 程序自身不要求管理员权限（`asInvoker`）。提权仅发生在用户选择计划任务自启时，通过 `Process.Start` + `Verb = "runas"` 临时提升
- **单实例**: `Mutex("WindowCenter_SingleInstance")` 防止重复启动
- **托盘菜单状态**: 通过 `RefreshAutoStartChecks` 同步当前自启模式到菜单勾选状态，切换时先清除后启用

## 边界情况

- 最大化/最小化窗口：先 `ShowWindow(SW_RESTORE)` + `Thread.Sleep(50)` 再取尺寸
- 全屏窗口：不跳过（`MONITOR_DEFAULTTONEAREST` 会正确返回显示器）
- 多显示器：`MonitorFromWindow` 自动匹配窗口当前所在显示器
- 任务栏避让：始终使用 `MONITORINFO.rcWork`
- 热键冲突：启动时注册失败静默处理，托盘菜单提供"重新注册"选项
