<div align="center">

# AutoSaving Alarm

### 固定周期提醒你保存工作的 Windows 托盘小工具

不接管编辑器，不绑定特定软件，也不做复杂自动同步。  
它只做一件事：在你最容易忘记保存的时候，稳定地提醒你一下。

[![Release](https://img.shields.io/github/v/release/zhujiu39/alarm-autosave?color=0969da&label=Release)](https://github.com/zhujiu39/alarm-autosave/releases)
[![Last Commit](https://img.shields.io/github/last-commit/zhujiu39/alarm-autosave?color=2da44e&label=Last%20Commit)](https://github.com/zhujiu39/alarm-autosave/commits/main)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D4)](https://github.com/zhujiu39/alarm-autosave)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Non--Commercial-orange)](LICENSE)

[下载 Release](https://github.com/zhujiu39/alarm-autosave/releases) |
[版本选择](#-下载与版本选择) |
[快速开始](#-快速开始) |
[FAQ](#-faq) |
[本地开发](#-本地开发)

</div>

> [!TIP]
> 如果你只是想直接用，不想研究 .NET 运行时，直接下载 `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip`。

## ✨ 一眼看懂

| 这个工具解决什么问题 | 它怎么工作 | 最适合谁 |
| --- | --- | --- |
| 你明明有手动保存习惯，但一专注就忘了按 `Ctrl + S` | 以固定周期提醒你保存，而不是每次确认后重新倒计时 | 写文档、写脚本、做设计、整理笔记时依赖手动保存的人 |

很多时候，真正让人崩溃的不是软件闪退，而是你已经做了二十分钟，结果忘了按一次 `Ctrl + S`。

`AutoSaving Alarm` 针对的就是这种场景：

- 常驻托盘，不打断主工作流
- 到点提醒，但不过度骚扰
- 保持固定节奏，而不是每次确认后重新倒计时

## 🖼️ 界面预览

| 设置窗口 | 到点提醒 |
| --- | --- |
| ![AutoSavingAlarm 设置窗口预览](assets/readme/settings-window.png) | ![AutoSavingAlarm 提醒窗口预览](assets/readme/reminder-window.png) |
| 首次启动时配置提醒间隔、恢复策略和开机启动 | 到点后通过提醒窗处理当前周期 |

## 🔥 核心特性

- **固定周期提醒**
  - 以“开始计时”或“应用新配置”的时刻作为锚点
  - 点击“我刚保存了”不会把整个提醒周期往后推
- **托盘常驻**
  - 默认运行在系统托盘
  - 双击托盘图标可打开设置
  - 托盘菜单支持恢复、暂停、确认已保存、设置和退出
- **双通道提醒**
  - 托盘气泡通知
  - 右下角置顶提醒窗
- **本地持久化**
  - 自动保存提醒间隔、恢复策略、暂停状态、开机启动等设置
- **开机自启动**
  - 使用当前用户注册表启动项
  - 不要求管理员权限
- **单实例保护**
  - 避免多个托盘进程重复运行

## 🧠 为什么它不是普通倒计时提醒器

普通提醒器常见逻辑是：

- 你点一次“知道了”
- 下一次提醒就从当前时刻重新开始计时

`AutoSaving Alarm` 不是这样。

它采用的是**固定周期提醒**。

例如：

- 你把提醒间隔设为 `15` 分钟
- 你在 `10:00` 开始
- 那么提醒点就是 `10:15`、`10:30`、`10:45`、`11:00`

如果你在 `10:17` 点击了“我刚保存了”，本次提醒会结束，但下一次依然是 `10:30`，不会顺延到 `10:32`。

这就是它最核心的设计点：**提醒节奏稳定、可预期。**

## 📦 下载与版本选择

Release 页面：

[https://github.com/zhujiu39/alarm-autosave/releases](https://github.com/zhujiu39/alarm-autosave/releases)

当前提供两个版本：

| 你的情况 | 建议下载 | 说明 |
| --- | --- | --- |
| 我只想下载后直接运行 | `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip` | 自带 Runtime，最省事，体积较大 |
| 我已经装过 `.NET 10 Windows Desktop Runtime` | `AutoSavingAlarm-v1.0.0-runtime-dependent-win-x64.zip` | 不自带 Runtime，体积较小 |
| 我不确定自己有没有装运行时 | `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip` | 这是默认推荐版本 |

## ⚡ 快速开始

### 第一次使用

1. 从 Releases 下载合适的版本
2. 启动程序
3. 设置提醒间隔
4. 选择是否开机启动
5. 选择恢复策略
6. 保存设置

如果本地还没有配置，程序会自动打开设置窗口。

### 日常使用

- 程序平时驻留在系统托盘
- 到点时会显示托盘提醒
- 同时弹出右下角置顶提醒窗
- 点击“我刚保存了”
  - 只结束当前提醒
  - 不重置整个周期
- 点击“暂停提醒”
  - 暂停后续提醒
  - 直到你手动恢复

### 恢复策略说明

| 策略 | 含义 |
| --- | --- |
| `恢复即重置` | 恢复提醒时，从当前时刻重新开始计时 |
| `沿用旧锚点` | 恢复提醒时，继续沿用之前的周期节奏 |

## 🎯 适用场景

这个工具尤其适合下面这些使用习惯：

- 写文档、写周报、整理笔记时依赖手动保存
- 写脚本、改配置、做原型时没有自动保存兜底
- 长时间专注工作，容易忽略保存动作
- 不想装复杂同步工具，只想要一个稳定提醒器

## ❓ FAQ

<details>
<summary><strong>为什么发布包这么大？</strong></summary>

因为 Release 同时提供了一个“自带 Runtime”的版本。

这个版本把 `.NET` 运行时一起打包进去了，优点是：

- 下载后直接运行
- 不要求用户自己安装依赖
- 更适合普通用户直接使用

代价就是体积会明显更大。  
如果你更在意体积，可以改用 `runtime-dependent` 版本。

</details>

<details>
<summary><strong>两个版本到底怎么选？</strong></summary>

最简单的判断方式：

- 想省事：下载 `self-contained`
- 知道自己机器已经装了 `.NET 10 Windows Desktop Runtime`：下载 `runtime-dependent`

如果你不能确定，就默认下载自带 Runtime 的版本。

</details>

<details>
<summary><strong>这个工具会自动帮我保存文件吗？</strong></summary>

不会。

它的职责是提醒你保存，而不是接管你的软件行为。  
这样做的好处是简单、稳定、兼容场景多，不需要适配具体编辑器，也不会误操作你的文件。

</details>

<details>
<summary><strong>它和普通倒计时提醒器最大的区别是什么？</strong></summary>

最大的区别是：**它保持固定节奏**。

普通倒计时提醒器通常在你每次确认后重新计时；`AutoSaving Alarm` 不会这样做。  
你确认“我刚保存了”后，只结束当前提醒，不改变下一次提醒的预定时间。

</details>

<details>
<summary><strong>关闭提醒窗会退出程序吗？</strong></summary>

不会。

程序的常驻入口在系统托盘。  
提醒窗只是当前周期的提醒界面，不是整个应用的主窗口。真正退出需要从托盘菜单里执行。

</details>

<details>
<summary><strong>支持开机自启动吗？需要管理员权限吗？</strong></summary>

支持。

当前版本使用当前用户注册表启动项：

- 路径：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 不要求管理员权限

</details>

<details>
<summary><strong>配置文件存在哪里？</strong></summary>

默认保存在：

```text
%AppData%\AutoSavingAlarm\settings.json
```

其中会记录：

- 提醒间隔
- 是否开机自启动
- 是否暂停
- 锚点时间
- 上次确认已保存时间
- 恢复策略

</details>

<details>
<summary><strong>支持 macOS 或 Linux 吗？</strong></summary>

当前不支持。  
目前版本只支持 `Windows`。

</details>

## 📝 更新日志

### v1.0.0

- 提供固定周期提醒，不因确认一次已保存而重置整体节奏
- 提供托盘常驻、托盘菜单和单实例保护
- 提供右下角提醒窗与托盘气泡通知
- 提供本地配置持久化与开机自启动
- 提供自带 Runtime 和依赖本机 Runtime 两种发布包

## 🗺️ 路线图

- [ ] 补一张托盘状态截图，让 README 的界面展示更完整
- [ ] 增加可选提示音
- [ ] 支持自定义提醒文案和提醒间隔预设
- [ ] 优化设置页说明文案，降低首次使用理解成本
- [ ] 增加基础自动化测试，覆盖配置损坏与启动项同步边界
- [ ] 继续打磨 README 的首屏视觉和下载指引

## 🧩 技术实现

核心模块包括：

- `TrayAppContext`
  - 管理托盘图标、菜单和程序生命周期
- `ReminderScheduler`
  - 负责固定周期提醒的计算与状态流转
- `SettingsStore`
  - 负责本地 JSON 配置读写
- `AutostartService`
  - 负责开机自启动注册表项
- `ReminderWindow`
  - 负责右下角提醒窗展示
- `SettingsForm`
  - 负责提醒间隔、恢复策略、自启动等设置

## 🗂️ 项目结构

```text
alarm-autosave/
├─ src/AutoSavingAlarm/
│  ├─ Application/      # 托盘生命周期与主流程
│  ├─ Configuration/    # 配置模型与持久化
│  ├─ Services/         # 调度、自启动等服务
│  └─ UI/               # 提醒窗、设置窗、图标
├─ artifacts/           # 本地发布产物
├─ assets/readme/       # README 使用的图片资源
├─ PLAN.md              # 方案说明与测试计划
├─ README.md
└─ LICENSE
```

## 🛠️ 本地开发

环境要求：

- Windows
- .NET 10 SDK

构建：

```powershell
dotnet build .\src\AutoSavingAlarm\AutoSavingAlarm.csproj
```

发布自带 Runtime 的版本：

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release -o .\artifacts\publish
```

发布依赖本机 Runtime 的轻量版本：

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o .\artifacts\publish-fd-single-explicit
```

## 🚧 已知边界

- 当前仅支持 Windows
- 当前不监听“你是不是真的保存了文件”
- 当前不支持 macOS
- 当前不支持 Linux
- 当前不提供云同步、声音提醒、全局快捷键

## 📄 许可证

本项目采用自定义非商用许可证：

- 允许个人使用
- 允许学习研究
- 允许修改和再分发
- 不允许商业用途

详见 [LICENSE](LICENSE)。
