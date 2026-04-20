using AutoSavingAlarm.Configuration;
using AutoSavingAlarm.Services;

namespace AutoSavingAlarm;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using SingleInstanceGuard singleInstanceGuard =
            SingleInstanceGuard.Acquire(@"Local\AutoSavingAlarm.SingleInstance");

        if (!singleInstanceGuard.IsPrimaryInstance)
        {
            MessageBox.Show(
                "AutoSavingAlarm 已经在系统托盘中运行。请在托盘图标上右键，或双击托盘图标打开设置。",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using SettingsStore settingsStore = new();
        using AutostartService autostartService = new();
        using TrayAppContext trayAppContext = new(settingsStore, autostartService);

        Application.Run(trayAppContext);
    }
}
