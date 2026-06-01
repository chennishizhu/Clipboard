# ClipVault

Windows 10 剪贴板管理工具，C# + WPF + SQLite。

## 编译要求

- Visual Studio 2022（或 Rider）
- .NET 8 SDK
- 目标平台：Windows 10+

## 快速开始

1. 用 Visual Studio 打开 `ClipVault.sln`
2. 还原 NuGet 包（自动）
3. **准备图标文件**（见下方说明）
4. 按 F5 运行

## 图标文件说明

项目引用了两个图标文件，需要手动放置到 `ClipVault/Resources/` 目录：

| 文件 | 用途 |
|------|------|
| `icon.ico` | 应用程序图标（窗口标题栏） |
| `tray.ico` | 系统托盘图标 |

可以使用任意 `.ico` 文件，或用在线工具将 PNG 转换为 ICO。
两个文件可以相同。

## 功能快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+V` | 全局唤出 / 隐藏主窗口（可在设置中修改） |
| `Esc` | 清空搜索框 / 关闭主窗口 |
| 单击卡片文字 | 复制内容并关闭窗口 |
| 单击 `⋯` | 展开详情和操作按钮 |

## 数据文件位置

- 数据库：`%AppData%\ClipVault\clipvault.db`
- 设置：`%AppData%\ClipVault\settings.json`

## NuGet 依赖

| 包 | 版本 |
|----|------|
| Hardcodet.NotifyIcon.Wpf | 1.1.0 |
| Microsoft.Data.Sqlite | 8.0.0 |
| ModernWpf.UI | 0.9.6 |
