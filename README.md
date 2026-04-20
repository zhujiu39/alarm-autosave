# AutoSaving Alarm

一个面向 Windows 的托盘常驻小工具，按固定周期提醒你保存当前工作，避免因为忘记手动保存而丢内容。

## 项目简介

很多编辑器、设计工具、文档工具并不会替你自动兜底，尤其是在草稿、临时文档、原型设计、脚本编辑这些场景里，一次忘记保存就可能白干很久。

`AutoSaving Alarm` 不尝试接管你的软件，也不监听具体应用，而是专注做好一件事：

- 常驻系统托盘
- 按固定间隔提醒你保存
- 你点击“我刚保存了”后，只结束本次提醒，不打乱整个提醒节奏

如果你已经形成了手动保存习惯，但又经常因为专注而忘记按 `Ctrl + S`，这个工具就是为这种场景准备的。

## 当前状态

- 当前仅支持 `Windows`
- 当前版本：`v1.0.0`
- 发布形式：单文件桌面程序
- 授权方式：禁止商业用途，其余允许学习、研究、修改和再分发

## 核心特性

- 固定周期提醒
  - 以“开始计时”或“应用新配置”的时刻作为锚点
  - 点击“我刚保存了”不会把整个提醒周期往后推
- 托盘常驻
  - 默认运行在系统托盘
  - 双击托盘图标可打开设置
  - 托盘菜单支持恢复、暂停、确认已保存、设置和退出
- 双通道提醒
  - 托盘气泡通知
  - 右下角置顶提醒窗
- 状态明确
  - 正常计时
  - 到点提醒
  - 已暂停
- 本地持久化
  - 自动保存提醒间隔、恢复策略、开机启动等设置
- 开机自启动
  - 使用当前用户注册表启动项
  - 不要求管理员权限
- 单实例保护
  - 避免重复启动多个托盘进程

## 适合谁

- 经常写文档、记笔记、写脚本，但依赖手动保存的人
- 用设计工具、编辑器或 IDE 时容易沉浸到忘记保存的人
- 不想装复杂同步工具，只想要一个简单提醒器的人

## 下载

Release 页面：

[https://github.com/zhujiu39/alarm-autosave/releases](https://github.com/zhujiu39/alarm-autosave/releases)

当前提供两个版本：

| 文件名 | 适合谁 | 说明 |
| --- | --- | --- |
| `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip` | 普通用户 | 自带 Runtime，下载后直接运行，体积较大 |
| `AutoSavingAlarm-v1.0.0-runtime-dependent-win-x64.zip` | 已装 .NET 的机器 | 不自带 Runtime，体积较小，要求本机已安装 `.NET 10 Windows Desktop Runtime` |

如果你不确定该下哪个，直接下载：

- `AutoSavingAlarm-v1.0.0-self-contained-win-x64.zip`

## 使用方式

### 首次启动

1. 启动程序
2. 设置提醒间隔
3. 选择是否开机自动启动
4. 选择恢复策略
5. 保存设置

首次启动如果没有配置，程序会直接打开设置窗口。

### 日常使用

- 到点后，托盘图标会切换为提醒状态
- 程序会弹出托盘提醒和右下角提醒窗
- 点击“我刚保存了”
  - 只结束当前提醒
  - 不重置整个固定周期
- 点击“暂停提醒”
  - 停止后续提醒
  - 直到你手动恢复
- 修改提醒间隔
  - 会以当前时刻重新建立新的提醒锚点

### 恢复策略

- `恢复即重置`
  - 恢复提醒时，从当前时刻重新开始计时
- `沿用旧锚点`
  - 恢复提醒时，继续沿用之前的周期节奏

## 程序如何工作

这个工具不是“每次点已保存后再重新倒计时”，而是“固定节奏提醒”。

举例：

- 你把提醒间隔设为 `15` 分钟
- 你在 `10:00` 开始计时
- 那么提醒点就是 `10:15`、`10:30`、`10:45`、`11:00`

如果你在 `10:17` 点击了“我刚保存了”，这次提醒会结束，但下一次仍然是 `10:30`，不会顺延到 `10:32`。

这是这个工具和普通倒计时提醒器最核心的区别。

## 配置文件

配置文件默认保存在：

```text
%AppData%\AutoSavingAlarm\settings.json
```

保存的内容包括：

- 提醒间隔
- 是否开机自启动
- 是否暂停
- 锚点时间
- 上次确认已保存时间
- 恢复策略

## 开发环境

- Windows
- .NET 10 SDK

## 本地构建

```powershell
dotnet build .\src\AutoSavingAlarm\AutoSavingAlarm.csproj
```

## 本地发布

自带 Runtime 的发布：

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release -o .\artifacts\publish
```

依赖本机 Runtime 的轻量发布：

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o .\artifacts\publish-fd-single-explicit
```

## 项目结构

```text
alarm-autosave/
├─ src/AutoSavingAlarm/
│  ├─ Application/      # 托盘生命周期与主流程
│  ├─ Configuration/    # 配置模型与持久化
│  ├─ Services/         # 调度、自启动等服务
│  └─ UI/               # 提醒窗、设置窗、图标
├─ artifacts/           # 本地发布产物
├─ PLAN.md              # 方案说明与测试计划
├─ README.md
└─ LICENSE
```

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

## 已知边界

- 当前仅支持 Windows
- 当前不监听“你是不是真的保存了文件”
- 当前不支持 macOS
- 当前不支持 Linux
- 当前不提供云同步、声音提醒、全局快捷键

## 路线方向

后续可以继续扩展的方向包括：

- 提醒文案与图标主题自定义
- 可选提示音
- 多套提醒配置
- 更完整的设置说明和截图

## 许可证

本项目采用自定义非商用许可证：

- 允许个人使用
- 允许学习研究
- 允许修改和再分发
- 不允许商业用途

详见 [LICENSE](LICENSE)。
