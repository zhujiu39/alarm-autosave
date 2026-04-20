# AutoSaving Alarm

一个面向 Windows 的托盘常驻小工具，用固定周期提醒你保存当前工作，避免因为忘记手动保存而丢内容。

## 这个项目解决什么问题

很多编辑器、设计工具、文档工具并不会替你兜底。`AutoSaving Alarm` 不尝试接管你的软件，也不监听具体应用，而是做一件简单但稳定的事：

- 常驻系统托盘
- 按固定间隔提醒你保存
- 你确认“我刚保存了”后，只结束本次提醒，不打乱整个提醒节奏
- 支持记住配置，支持开机自启动

如果你习惯手动保存，但又经常因为专注工作而忘记按 `Ctrl + S`，这个工具就是给这种场景准备的。

## 功能特性

- 固定周期提醒
  - 以“开始计时”或“应用新配置”的时刻作为锚点
  - 点击“我刚保存了”不会把下次提醒整体往后推
- 原生 Windows 托盘应用
  - 无主窗口常驻
  - 双击托盘图标可打开设置
  - 托盘菜单支持恢复、暂停、确认已保存、设置、退出
- 双通道提醒
  - 托盘气泡通知
  - 右下角置顶提醒窗
- 状态清晰
  - 正常计时
  - 到点提醒
  - 已暂停
- 本地持久化
  - 配置保存到当前用户目录
  - 重启应用后可恢复上次状态
- 开机自启动
  - 使用当前用户注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  - 不要求管理员权限
- 单实例保护
  - 避免重复启动多个托盘进程

## 运行方式

### 直接下载

可在 GitHub 的 Releases 页面下载打包好的单文件程序：

[Releases](https://github.com/zhujiu39/alarm-autosave/releases)

首次启动时，如果还没有配置，程序会直接打开设置窗口。

### 自行构建

环境要求：

- Windows
- .NET 10 SDK

构建：

```powershell
dotnet build .\src\AutoSavingAlarm\AutoSavingAlarm.csproj
```

发布：

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release -o .\artifacts\publish
```

## 使用说明

### 首次启动

1. 启动程序
2. 设置提醒间隔
3. 选择是否开机自动启动
4. 保存配置

### 日常使用

- 到点后，托盘图标会变为提醒状态，同时弹出提醒窗
- 点击“我刚保存了”：
  - 只关闭当前提醒
  - 不重置整个固定周期
- 点击“暂停提醒”：
  - 停止后续提醒
  - 直到你手动恢复
- 修改提醒间隔：
  - 会以当前时刻重新建立新的提醒锚点

### 配置文件位置

配置文件保存在：

```text
%AppData%\AutoSavingAlarm\settings.json
```

## 项目结构

```text
alarm-autosave/
├─ src/AutoSavingAlarm/          # WinForms 应用源码
│  ├─ Application/              # 托盘生命周期与主流程
│  ├─ Configuration/            # 配置模型与持久化
│  ├─ Services/                 # 调度、自启动等服务
│  └─ UI/                       # 提醒窗、设置窗、图标
├─ artifacts/                   # 本地发布产物
├─ PLAN.md                      # 方案与测试计划
├─ README.md
└─ LICENSE
```

## 实现说明

核心模块：

- `TrayAppContext`
  - 管理托盘图标、菜单、程序生命周期
- `ReminderScheduler`
  - 计算固定周期提醒
  - 处理暂停、恢复、确认已保存等行为
- `SettingsStore`
  - 使用 JSON 读写用户配置
- `AutostartService`
  - 处理开机自启动注册表项
- `ReminderWindow`
  - 展示右下角置顶提醒窗口
- `SettingsForm`
  - 提供提醒间隔、恢复策略、自启动等设置

设计取向：

- 不侵入具体软件
- 不依赖复杂后台服务
- 优先保证提醒节奏稳定、行为可预期

## 当前版本

- `v1.0.0`
  - 首个公开版本
  - 提供固定周期提醒、托盘驻留、设置持久化、自启动与单文件发布

## 许可证

本项目采用自定义的非商用许可证，允许个人、学习、研究、修改和再分发，但不允许任何商业用途。

详见 [LICENSE](LICENSE)。
