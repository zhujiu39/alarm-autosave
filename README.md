# AutoSaving Alarm

一个面向 Windows 的托盘常驻小工具，用固定节奏提醒你保存当前工作。

很多时候，真正让人崩溃的不是软件崩掉，而是你明明已经做了二十分钟，结果忘了按一次 `Ctrl + S`。

`AutoSaving Alarm` 不接管你的编辑器，不绑定某个特定软件，也不做复杂自动同步。它只做一件事：在你最容易忘记保存的时候，稳定地提醒你一下。

## 3 秒看懂

- 这是一个 `Windows` 托盘小工具
- 它会按固定间隔提醒你保存
- 你点“我刚保存了”后，只结束本次提醒，不会把整个提醒节奏往后推

如果你不想研究细节，直接去 Releases 下载 `自带 Runtime` 的版本即可：

[下载 Release](https://github.com/zhujiu39/alarm-autosave/releases)

## 界面预览

首次启动时会打开设置窗口：

![AutoSavingAlarm 设置窗口预览](assets/readme/settings-window.png)

到点后会弹出右下角提醒窗：

![AutoSavingAlarm 提醒窗口预览](assets/readme/reminder-window.png)

## 它解决的是什么问题

很多工具并不会帮你自动兜底，尤其是下面这些场景：

- 临时草稿
- 本地脚本
- 原型设计
- 笔记整理
- 非自动保存的软件或文件

你明明有“手动保存”的习惯，但一旦进入专注状态，就会自然忽略掉这一步。

这个工具就是为这种情况准备的：

- 常驻托盘，不打断主工作流
- 到点提醒，但不过度骚扰
- 保持固定节奏，而不是每次确认后重新倒计时

## 为什么和普通倒计时提醒器不一样

普通提醒器常见逻辑是：

- 你点一次“知道了”
- 下一次提醒就从当前时刻重新开始计时

`AutoSaving Alarm` 不是这样。

它采用的是“固定周期提醒”。

举例：

- 你把提醒间隔设为 `15` 分钟
- 你在 `10:00` 开始
- 那么提醒点就是 `10:15`、`10:30`、`10:45`、`11:00`

如果你在 `10:17` 点击“我刚保存了”，本次提醒会结束，但下一次依然是 `10:30`，不会顺延到 `10:32`。

这就是它最核心的设计点：提醒节奏稳定、可预期。

## 下载说明

Release 页面：

[https://github.com/zhujiu39/alarm-autosave/releases](https://github.com/zhujiu39/alarm-autosave/releases)

当前提供两个版本：

| 文件名 | 适合谁 | 说明 |
| --- | --- | --- |
| `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip` | 普通用户 | 自带 Runtime，下载后直接运行，体积较大 |
| `AutoSavingAlarm-v1.0.0-runtime-dependent-win-x64.zip` | 已装 .NET 的机器 | 不自带 Runtime，体积较小，要求本机已安装 `.NET 10 Windows Desktop Runtime` |

如果你不确定下哪个，直接选：

- `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip`

## 快速开始

### 首次使用

1. 下载并启动程序
2. 设定提醒间隔
3. 选择是否开机启动
4. 选择恢复策略
5. 保存设置

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

## 核心特性

- 固定周期提醒
  - 以“开始计时”或“应用新配置”的时刻作为锚点
- 托盘常驻
  - 双击托盘图标可打开设置
- 双通道提醒
  - 托盘气泡通知 + 右下角提醒窗
- 本地持久化
  - 自动保存提醒间隔、恢复策略、暂停状态、开机启动等设置
- 开机自启动
  - 使用当前用户注册表启动项
  - 不要求管理员权限
- 单实例保护
  - 避免多个托盘进程重复运行

## 适合谁

- 经常写文档、写脚本、记笔记，但依赖手动保存的人
- 经常进入专注状态，容易忘记保存的人
- 不想装复杂同步工具，只想要一个简单提醒器的人

## FAQ

### 为什么发布包这么大？

因为 Release 同时提供了一个“自带 Runtime”的版本。

这个版本把 `.NET` 运行时一起打包进去了，优点是：

- 下载后直接运行
- 不要求用户自己安装依赖

代价就是体积会明显更大。

### 两个版本怎么选？

直接按这个规则选：

- 想省事：下载 `self-contained`
- 知道自己机器已经装了 `.NET 10 Windows Desktop Runtime`：下载 `runtime-dependent`

如果你不确定，就下自带 Runtime 的版本。

### 这个工具会自动帮我保存文件吗？

不会。

它的职责是提醒你保存，而不是接管你的软件行为。这样做的好处是简单、稳定、兼容场景多，不需要适配具体编辑器。

### 它支持 macOS 或 Linux 吗？

当前不支持。

目前版本只支持 `Windows`。

## 配置文件

配置文件默认保存在：

```text
%AppData%\AutoSavingAlarm\settings.json
```

配置内容包括：

- 提醒间隔
- 是否开机自启动
- 是否暂停
- 锚点时间
- 上次确认已保存时间
- 恢复策略

## 技术实现

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

## 项目结构

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

## 本地开发

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

## 已知边界

- 当前仅支持 Windows
- 当前不监听“你是不是真的保存了文件”
- 当前不支持 macOS
- 当前不支持 Linux
- 当前不提供云同步、声音提醒、全局快捷键

## 许可证

本项目采用自定义非商用许可证：

- 允许个人使用
- 允许学习研究
- 允许修改和再分发
- 不允许商业用途

详见 [LICENSE](LICENSE)。
