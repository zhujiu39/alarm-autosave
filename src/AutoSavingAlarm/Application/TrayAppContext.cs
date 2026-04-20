using AutoSavingAlarm.Configuration;
using AutoSavingAlarm.Services;
using AutoSavingAlarm.UI;
using Microsoft.Win32;

namespace AutoSavingAlarm;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly AutostartService _autostartService;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly ToolStripMenuItem _startResumeMenuItem;
    private readonly ToolStripMenuItem _pauseMenuItem;
    private readonly ToolStripMenuItem _acknowledgeMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _autostartMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private readonly System.Windows.Forms.Timer _tickTimer;
    private readonly System.Windows.Forms.Timer _startupTimer;
    private readonly ReminderWindow _reminderWindow;
    private readonly Icon _normalIcon;
    private readonly Icon _alertIcon;
    private readonly Icon _pausedIcon;
    private AppSettings _settings;
    private ReminderScheduler _scheduler;
    private readonly bool _hasSavedConfig;
    private bool _isExiting;

    public TrayAppContext(SettingsStore settingsStore, AutostartService autostartService)
    {
        _settingsStore = settingsStore;
        _autostartService = autostartService;

        (AppSettings settings, bool hasSavedConfig) = _settingsStore.Load();
        _settings = settings;
        _scheduler = new ReminderScheduler(_settings);
        _hasSavedConfig = hasSavedConfig;

        _normalIcon = IconFactory.CreateStatusIcon(Color.FromArgb(54, 148, 92));
        _alertIcon = IconFactory.CreateStatusIcon(Color.FromArgb(220, 91, 41));
        _pausedIcon = IconFactory.CreateStatusIcon(Color.FromArgb(123, 123, 123));

        _trayMenu = new ContextMenuStrip();
        _startResumeMenuItem = new ToolStripMenuItem("立即开始/恢复");
        _pauseMenuItem = new ToolStripMenuItem("暂停提醒");
        _acknowledgeMenuItem = new ToolStripMenuItem("我刚保存了");
        _settingsMenuItem = new ToolStripMenuItem("设置");
        _autostartMenuItem = new ToolStripMenuItem("开机自启动");
        _exitMenuItem = new ToolStripMenuItem("退出");

        _startResumeMenuItem.Click += (_, _) => StartOrResume();
        _pauseMenuItem.Click += (_, _) => PauseReminders();
        _acknowledgeMenuItem.Click += (_, _) => AcknowledgeReminder();
        _settingsMenuItem.Click += (_, _) => OpenSettings(isFirstRun: false);
        _autostartMenuItem.Click += (_, _) => ToggleAutostart();
        _exitMenuItem.Click += (_, _) => ExitApplication();

        _trayMenu.Items.AddRange(
        [
            _startResumeMenuItem,
            _pauseMenuItem,
            _acknowledgeMenuItem,
            new ToolStripSeparator(),
            _settingsMenuItem,
            _autostartMenuItem,
            new ToolStripSeparator(),
            _exitMenuItem
        ]);

        _notifyIcon = new NotifyIcon
        {
            Text = "AutoSavingAlarm",
            ContextMenuStrip = _trayMenu,
            Visible = true,
            Icon = _normalIcon
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings(isFirstRun: false);

        _reminderWindow = new ReminderWindow();
        _reminderWindow.SaveAcknowledgedRequested += (_, _) => AcknowledgeReminder();
        _reminderWindow.PauseRequested += (_, _) => PauseReminders();
        _reminderWindow.OpenSettingsRequested += (_, _) => OpenSettings(isFirstRun: false);

        _tickTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        _tickTimer.Tick += (_, _) => RefreshState(showBalloonTip: true);
        _tickTimer.Start();

        _startupTimer = new System.Windows.Forms.Timer
        {
            Interval = 1
        };
        _startupTimer.Tick += StartupTimer_Tick;
        _startupTimer.Start();

        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        TrySyncAutostartWithSettings();
    }

    protected override void ExitThreadCore()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

        _tickTimer.Stop();
        _tickTimer.Dispose();
        _startupTimer.Stop();
        _startupTimer.Dispose();

        _reminderWindow.PrepareForAppExit();
        _reminderWindow.Close();
        _reminderWindow.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayMenu.Dispose();

        _normalIcon.Dispose();
        _alertIcon.Dispose();
        _pausedIcon.Dispose();

        base.ExitThreadCore();
    }

    private void RefreshState(bool showBalloonTip)
    {
        if (_isExiting)
        {
            return;
        }

        ReminderSnapshot snapshot = _scheduler.Evaluate(DateTimeOffset.UtcNow);
        UpdateTrayStatus(snapshot);
        UpdateReminderWindow(snapshot);

        if (showBalloonTip && snapshot.ReminderTriggered)
        {
            _notifyIcon.BalloonTipTitle = "该保存了";
            _notifyIcon.BalloonTipText = "到固定保存时间了。点击托盘或提醒窗都可以处理。";
            _notifyIcon.ShowBalloonTip(3000);
        }
    }

    private void UpdateTrayStatus(ReminderSnapshot snapshot)
    {
        switch (snapshot.State)
        {
            case ReminderState.Normal:
                _notifyIcon.Icon = _normalIcon;
                _notifyIcon.Text = BuildNotifyText("正常计时中", snapshot.NextReminderUtc);
                break;
            case ReminderState.Reminder:
                _notifyIcon.Icon = _alertIcon;
                _notifyIcon.Text = "AutoSavingAlarm - 该保存了";
                break;
            default:
                _notifyIcon.Icon = _pausedIcon;
                _notifyIcon.Text = "AutoSavingAlarm - 已暂停";
                break;
        }

        _startResumeMenuItem.Text = _settings.IsPaused ? "立即开始/恢复" : "重新开始计时";
        _pauseMenuItem.Enabled = !_settings.IsPaused;
        _acknowledgeMenuItem.Enabled = snapshot.State == ReminderState.Reminder;
        _autostartMenuItem.Checked = _settings.StartWithWindows;
    }

    private void UpdateReminderWindow(ReminderSnapshot snapshot)
    {
        if (snapshot.State != ReminderState.Reminder)
        {
            if (_reminderWindow.Visible)
            {
                _reminderWindow.Hide();
            }

            return;
        }

        _reminderWindow.UpdatePresentation(snapshot, _settings);

        if (!_reminderWindow.Visible)
        {
            _reminderWindow.Show();
        }

        _reminderWindow.Activate();
    }

    private void StartOrResume()
    {
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;

        if (_settings.IsPaused)
        {
            _scheduler.Resume(nowUtc);
        }
        else
        {
            _scheduler.ResetAnchor(nowUtc);
        }

        SaveSettingsOrReport();
        RefreshState(showBalloonTip: false);
    }

    private void PauseReminders()
    {
        _scheduler.Pause();
        SaveSettingsOrReport();
        RefreshState(showBalloonTip: false);
    }

    private void AcknowledgeReminder()
    {
        _scheduler.Acknowledge(DateTimeOffset.UtcNow);
        SaveSettingsOrReport();
        RefreshState(showBalloonTip: false);
    }

    private void ToggleAutostart()
    {
        bool previousValue = _settings.StartWithWindows;
        _settings.StartWithWindows = !previousValue;

        try
        {
            _autostartService.SetEnabled(_settings.StartWithWindows);
            SaveSettingsOrReport();
        }
        catch (Exception exception)
        {
            _settings.StartWithWindows = previousValue;
            MessageBox.Show(
                $"更新开机自启动失败：{exception.Message}",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        RefreshState(showBalloonTip: false);
    }

    private void OpenSettings(bool isFirstRun)
    {
        using SettingsForm settingsForm = new(_settings);
        settingsForm.Activate();
        DialogResult dialogResult = settingsForm.ShowDialog();

        if (dialogResult != DialogResult.OK || settingsForm.SubmittedSettings is null)
        {
            if (isFirstRun)
            {
                ExitApplication();
            }

            return;
        }

        ApplySettings(settingsForm.SubmittedSettings, isFirstRun);
    }

    private void ApplySettings(AppSettings requestedSettings, bool isFirstRun)
    {
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        bool intervalChanged = requestedSettings.IntervalMinutes != _settings.IntervalMinutes;
        bool wasPaused = _settings.IsPaused;
        bool previousStartWithWindows = _settings.StartWithWindows;

        AppSettings updatedSettings = _settings.Clone();
        updatedSettings.IntervalMinutes = requestedSettings.IntervalMinutes;
        updatedSettings.StartWithWindows = requestedSettings.StartWithWindows;
        updatedSettings.IsPaused = requestedSettings.IsPaused;
        updatedSettings.ResumePolicy = requestedSettings.ResumePolicy;

        _settings = updatedSettings;
        _scheduler = new ReminderScheduler(_settings);

        if (isFirstRun || intervalChanged)
        {
            _scheduler.ResetAnchor(nowUtc);
        }
        else if (!requestedSettings.IsPaused && wasPaused)
        {
            _scheduler.Resume(nowUtc);
        }

        if (requestedSettings.IsPaused)
        {
            _scheduler.Pause();
        }

        try
        {
            _autostartService.SetEnabled(_settings.StartWithWindows);
        }
        catch (Exception exception)
        {
            _settings.StartWithWindows = previousStartWithWindows;
            MessageBox.Show(
                $"更新开机自启动失败：{exception.Message}",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        SaveSettingsOrReport();
        RefreshState(showBalloonTip: false);
    }

    private void TrySyncAutostartWithSettings()
    {
        try
        {
            bool registryEnabled = _autostartService.IsEnabled();
            if (registryEnabled != _settings.StartWithWindows)
            {
                _autostartService.SetEnabled(_settings.StartWithWindows);
            }
        }
        catch
        {
        }
    }

    private void SaveSettingsOrReport()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"保存配置失败：{exception.Message}",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitApplication()
    {
        ExitThread();
    }

    private void SystemEvents_PowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            RefreshState(showBalloonTip: true);
        }
    }

    private void StartupTimer_Tick(object? sender, EventArgs e)
    {
        _startupTimer.Stop();

        if (_hasSavedConfig)
        {
            RefreshState(showBalloonTip: true);
            return;
        }

        OpenSettings(isFirstRun: true);
    }

    private static string BuildNotifyText(string stateText, DateTimeOffset? nextReminderUtc)
    {
        if (!nextReminderUtc.HasValue)
        {
            return $"AutoSavingAlarm - {stateText}";
        }

        string nextTime = nextReminderUtc.Value.ToLocalTime().ToString("HH:mm");
        return $"AutoSavingAlarm - {stateText} - 下次 {nextTime}";
    }
}
