# AutoSaving Alarm

一个常驻系统托盘的原生 Windows 小工具，用来按固定周期提醒你保存当前工作。

## 功能

- 托盘常驻运行，支持单实例保护
- 按固定间隔触发提醒，不因“我刚保存了”而重置整个周期
- 到点时切换托盘图标状态，并弹出提醒窗口
- 支持保存本地配置与开机自启动

## 项目结构

- `src/AutoSavingAlarm`：WinForms 应用源码
- `PLAN.md`：方案说明与测试计划
- `artifacts/`：本地发布产物目录，已排除版本控制

## 开发环境

- Windows
- .NET 10.0 SDK

## 构建

```powershell
dotnet build .\src\AutoSavingAlarm\AutoSavingAlarm.csproj
```

## 发布

```powershell
dotnet publish .\src\AutoSavingAlarm\AutoSavingAlarm.csproj -c Release
```
