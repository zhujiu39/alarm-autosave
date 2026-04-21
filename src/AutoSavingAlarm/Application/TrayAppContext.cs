using AutoSavingAlarm.Configuration;
using AutoSavingAlarm.Services;
using AutoSavingAlarm.UI;
using Microsoft.Win32;
using System.Media;

namespace AutoSavingAlarm;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly AutostartService _autostartService;
    private readonly WorkScheduleEvaluator _workScheduleEvaluator;
    private readonly UserIdleMonitor _idleMonitor;
    private readonly SettingsLoadResult _loadResult;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly ToolStripMenuItem _startResumeMenuItem;
    private readonly ToolStripMenuItem _pauseMenuItem;
    private readonly ToolStripMenuItem _acknowledgeMenuItem;
    private readonly ToolStripMenuItem _snoozeMenuItem;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _autostartMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private readonly System.Windows.Forms.Timer _tickTimer;
    private readonly System.Windows.Forms.Timer _startupTimer;
    private readonly ReminderWindow _reminderWindow;
    private readonly Icon _normalIcon;
    private readonly Icon _alertIcon;
    private readonly Icon _pausedIcon;
    private readonly Icon _snoozedIcon;
    private AppSettings _settings;
    private ReminderScheduler _scheduler;
    private readonly bool _hasSavedConfig;
    private bool _isExiting;
    private DateTimeOffset? _lastEscalatedSoundAtUtc;

    public TrayAppContext(SettingsStore settingsStore, AutostartService autostartService)
    {
        _settingsStore = settingsStore;
        _autostartService = autostartService;
        _workScheduleEvaluator = new WorkScheduleEvaluator();
        _idleMonitor = new UserIdleMonitor();

        _loadResult = _settingsStore.Load();
        _settings = _loadResult.Settings;
        _scheduler = new ReminderScheduler(_settings, _workScheduleEvaluator);
        _hasSavedConfig = _loadResult.Exists;

        _normalIcon = IconFactory.CreateStatusIcon(Color.FromArgb(54, 148, 92));
        _alertIcon = IconFactory.CreateStatusIcon(Color.FromArgb(220, 91, 41));
        _pausedIcon = IconFactory.CreateStatusIcon(Color.FromArgb(123, 123, 123));
        _snoozedIcon = IconFactory.CreateStatusIcon(Color.FromArgb(72, 122, 204));

        _trayMenu = new ContextMenuStrip();
        _startResumeMenuItem = new ToolStripMenuItem("立即开始/恢复");
        _pauseMenuItem = new ToolStripMenuItem("暂停提醒");
        _acknowledgeMenuItem = new ToolStripMenuItem("我已保存");
        _snoozeMenuItem = new ToolStripMenuItem(BuildSnoozeMenuText(_settings.DefaultSnoozeMinutes));
        _settingsMenuItem = new ToolStripMenuItem("设置");
        _autostartMenuItem = new ToolStripMenuItem("开机自启动");
        _exitMenuItem = new ToolStripMenuItem("退出");

        _startResumeMenuItem.Click += (_, _) => StartOrResume();
        _pauseMenuItem.Click += (_, _) => PauseReminders();
        _acknowledgeMenuItem.Click += (_, _) => AcknowledgeReminder();
        _snoozeMenuItem.Click += (_, _) => SnoozeReminder();
        _settingsMenuItem.Click += (_, _) => OpenSettings(isFirstRun: false);
        _autostartMenuItem.Click += (_, _) => ToggleAutostart();
        _exitMenuItem.Click += (_, _) => ExitApplication();

        _trayMenu.Items.AddRange(
        [
            _startResumeMenuItem,
            _pauseMenuItem,
            _acknowledgeMenuItem,
            _snoozeMenuItem,
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
        _reminderWindow.SnoozeRequested += (_, _) => SnoozeReminder();
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
        _snoozedIcon.Dispose();

        base.ExitThreadCore();
    }

    private void RefreshState(bool showBalloonTip)
    {
        if (_isExiting)
        {
            return;
        }

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        bool isIdle = _idleMonitor.IsIdle(_settings);
        ReminderSnapshot snapshot = _scheduler.Evaluate(nowUtc, isIdle);

        if (snapshot.SettingsChanged)
        {
            SaveSettingsOrReport();
        }

        UpdateTrayStatus(snapshot);
        UpdateReminderWindow(snapshot);
        HandleReminderSound(snapshot, nowUtc);

        if (showBalloonTip && (snapshot.ReminderTriggered || snapshot.EscalationAdvanced))
        {
            _notifyIcon.BalloonTipTitle = BuildReminderTitle(snapshot.EscalationLevel);
            _notifyIcon.BalloonTipText = BuildReminderBalloonText(snapshot);
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
                _notifyIcon.Text = BuildReminderNotifyText(snapshot);
                break;
            case ReminderState.Snoozed:
                _notifyIcon.Icon = _snoozedIcon;
                _notifyIcon.Text = BuildSnoozedNotifyText(snapshot.SnoozeUntilUtc);
                break;
            case ReminderState.SuppressedBySchedule:
                _notifyIcon.Icon = _pausedIcon;
                _notifyIcon.Text = "AutoSavingAlarm - 非工作时段";
                break;
            case ReminderState.SuppressedByIdle:
                _notifyIcon.Icon = _pausedIcon;
                _notifyIcon.Text = "AutoSavingAlarm - 空闲中已挂起";
                break;
            default:
                _notifyIcon.Icon = _pausedIcon;
                _notifyIcon.Text = "AutoSavingAlarm - 已手动暂停";
                break;
        }

        _startResumeMenuItem.Text = _settings.IsPaused ? "立即开始/恢复" : "重新开始计时";
        _pauseMenuItem.Enabled = !_settings.IsPaused;
        _acknowledgeMenuItem.Enabled = snapshot.State == ReminderState.Reminder;
        _snoozeMenuItem.Enabled = snapshot.State == ReminderState.Reminder;
        _snoozeMenuItem.Text = BuildSnoozeMenuText(_settings.DefaultSnoozeMinutes);
        _autostartMenuItem.Checked = _settings.StartWithWindows;
    }

    private void UpdateReminderWindow(ReminderSnapshot snapshot)
    {
        if (snapshot.State != ReminderState.Reminder)
        {
            _reminderWindow.SetReminderInactive();
            if (_reminderWindow.Visible)
            {
                _reminderWindow.Hide();
            }

            return;
        }

        bool shouldActivateWindow =
            !_reminderWindow.Visible ||
            snapshot.ReminderTriggered ||
            snapshot.EscalationAdvanced ||
            snapshot.ResumedFromSnooze;

        _reminderWindow.UpdatePresentation(snapshot, _settings);

        if (!_reminderWindow.Visible)
        {
            _reminderWindow.Show();
        }

        if (shouldActivateWindow)
        {
            _reminderWindow.BringToFront();
            _reminderWindow.Activate();
        }
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

    private void SnoozeReminder()
    {
        _scheduler.Snooze(DateTimeOffset.UtcNow, _settings.DefaultSnoozeMinutes);
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
        using SettingsForm settingsForm = new(_settings, _settingsStore);
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
        bool wasPaused = _settings.IsPaused;
        bool previousStartWithWindows = _settings.StartWithWindows;
        bool timingSettingsChanged = HaveTimingSettingsChanged(_settings, requestedSettings);

        AppSettings updatedSettings = _settings.Clone();
        updatedSettings.IntervalMinutes = requestedSettings.IntervalMinutes;
        updatedSettings.AcknowledgeResetsCycle = requestedSettings.AcknowledgeResetsCycle;
        updatedSettings.SoundEnabled = requestedSettings.SoundEnabled;
        updatedSettings.DefaultSnoozeMinutes = requestedSettings.DefaultSnoozeMinutes;
        updatedSettings.StartWithWindows = requestedSettings.StartWithWindows;
        updatedSettings.IsPaused = requestedSettings.IsPaused;
        updatedSettings.ResumePolicy = requestedSettings.ResumePolicy;
        updatedSettings.WorkScheduleEnabled = requestedSettings.WorkScheduleEnabled;
        updatedSettings.WorkdayMask = requestedSettings.WorkdayMask;
        updatedSettings.WorkdayStartLocalTime = requestedSettings.WorkdayStartLocalTime;
        updatedSettings.WorkdayEndLocalTime = requestedSettings.WorkdayEndLocalTime;
        updatedSettings.IdleDetectionEnabled = requestedSettings.IdleDetectionEnabled;
        updatedSettings.IdleThresholdMinutes = requestedSettings.IdleThresholdMinutes;
        updatedSettings.SnoozeUntilUtc = null;

        _settings = updatedSettings;
        _scheduler = new ReminderScheduler(_settings, _workScheduleEvaluator);

        if (requestedSettings.IsPaused)
        {
            _scheduler.Pause();
        }
        else if (isFirstRun || timingSettingsChanged)
        {
            _scheduler.ResetAnchor(nowUtc);
        }
        else if (wasPaused)
        {
            _scheduler.Resume(nowUtc);
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
        if (!_hasSavedConfig || _loadResult.Source != SettingsLoadSource.Primary)
        {
            return;
        }

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
            ShowStartupNoticeIfNeeded();
            return;
        }

        OpenSettings(isFirstRun: true);
    }

    private void ShowStartupNoticeIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_loadResult.Notice))
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "AutoSavingAlarm";
        _notifyIcon.BalloonTipText = _loadResult.Notice;
        _notifyIcon.ShowBalloonTip(4000);
    }

    private static bool HaveTimingSettingsChanged(AppSettings currentSettings, AppSettings requestedSettings)
    {
        return currentSettings.IntervalMinutes != requestedSettings.IntervalMinutes ||
               currentSettings.WorkScheduleEnabled != requestedSettings.WorkScheduleEnabled ||
               currentSettings.WorkdayMask != requestedSettings.WorkdayMask ||
               currentSettings.WorkdayStartLocalTime != requestedSettings.WorkdayStartLocalTime ||
               currentSettings.WorkdayEndLocalTime != requestedSettings.WorkdayEndLocalTime ||
               currentSettings.IdleDetectionEnabled != requestedSettings.IdleDetectionEnabled ||
               currentSettings.IdleThresholdMinutes != requestedSettings.IdleThresholdMinutes;
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

    private static string BuildSnoozeMenuText(int minutes)
    {
        return $"稍后提醒 {Math.Max(1, minutes)} 分钟";
    }

    private static string BuildSnoozedNotifyText(DateTimeOffset? snoozeUntilUtc)
    {
        if (!snoozeUntilUtc.HasValue)
        {
            return "AutoSavingAlarm - 已稍后提醒";
        }

        return $"AutoSavingAlarm - 已稍后至 {snoozeUntilUtc.Value.ToLocalTime():HH:mm}";
    }

    private void HandleReminderSound(ReminderSnapshot snapshot, DateTimeOffset nowUtc)
    {
        if (!_settings.SoundEnabled || snapshot.State != ReminderState.Reminder)
        {
            _lastEscalatedSoundAtUtc = null;
            return;
        }

        if (snapshot.EscalationLevel >= 3)
        {
            if (snapshot.ReminderTriggered ||
                snapshot.EscalationAdvanced ||
                snapshot.ResumedFromSnooze ||
                !_lastEscalatedSoundAtUtc.HasValue ||
                nowUtc - _lastEscalatedSoundAtUtc.Value >= TimeSpan.FromSeconds(30))
            {
                SystemSounds.Hand.Play();
                _lastEscalatedSoundAtUtc = nowUtc;
            }

            return;
        }

        if (snapshot.EscalationLevel == 2 && (snapshot.EscalationAdvanced || snapshot.ResumedFromSnooze))
        {
            SystemSounds.Exclamation.Play();
            _lastEscalatedSoundAtUtc = nowUtc;
            return;
        }

        _lastEscalatedSoundAtUtc = null;
    }

    private static string BuildReminderTitle(int escalationLevel)
    {
        return escalationLevel switch
        {
            >= 3 => "请立即保存",
            2 => "还没保存",
            _ => "该保存了"
        };
    }

    private static string BuildReminderBalloonText(ReminderSnapshot snapshot)
    {
        if (snapshot.ResumedFromSnooze)
        {
            return snapshot.OverdueCycles <= 1
                ? "稍后提醒已结束，请记得保存。"
                : $"稍后提醒已结束，当前已连续 {snapshot.OverdueCycles} 个周期未确认。";
        }

        if (snapshot.OverdueCycles <= 1)
        {
            return "到保存时间了。点击“我已保存”或使用“稍后提醒”处理当前提醒。";
        }

        return $"已连续 {snapshot.OverdueCycles} 个周期未确认，请尽快保存。";
    }

    private static string BuildReminderNotifyText(ReminderSnapshot snapshot)
    {
        if (snapshot.OverdueCycles <= 1)
        {
            return "AutoSavingAlarm - 该保存了";
        }

        return $"AutoSavingAlarm - 已连续错过 {snapshot.OverdueCycles} 次";
    }
}
