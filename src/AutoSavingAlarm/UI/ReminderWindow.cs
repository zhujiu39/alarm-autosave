using AutoSavingAlarm.Configuration;
using AutoSavingAlarm.Services;

namespace AutoSavingAlarm.UI;

internal sealed class ReminderWindow : Form
{
    private readonly Label _titleLabel;
    private readonly Label _intervalLabel;
    private readonly Label _currentCycleLabel;
    private readonly Label _nextCycleLabel;
    private readonly Label _statusLabel;
    private readonly Label _modeLabel;
    private readonly Label _countdownLabel;
    private readonly Label _noticeLabel;
    private readonly Button _acknowledgeButton;
    private readonly Button _snoozeButton;
    private readonly Button _pauseButton;
    private readonly Button _settingsButton;
    private readonly System.Windows.Forms.Timer _attentionTimer;
    private bool _allowClose;
    private int _currentEscalationLevel;
    private bool _attentionPulseOn;

    public ReminderWindow()
    {
        Text = "AutoSavingAlarm";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ClientSize = new Size(560, 296);

        _titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 46,
            Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
            Padding = new Padding(14, 8, 14, 0),
            Text = "该保存了"
        };

        _intervalLabel = CreateBodyLabel();
        _currentCycleLabel = CreateBodyLabel();
        _nextCycleLabel = CreateBodyLabel();
        _statusLabel = CreateBodyLabel();
        _modeLabel = CreateBodyLabel();
        _countdownLabel = CreateBodyLabel();
        _noticeLabel = CreateBodyLabel();

        _acknowledgeButton = new Button
        {
            AutoSize = false,
            Size = new Size(102, 34),
            Text = "我已保存"
        };
        _acknowledgeButton.Click += (_, _) => SaveAcknowledgedRequested?.Invoke(this, EventArgs.Empty);

        _snoozeButton = new Button
        {
            AutoSize = false,
            Size = new Size(102, 34),
            Text = "稍后提醒"
        };
        _snoozeButton.Click += (_, _) => SnoozeRequested?.Invoke(this, EventArgs.Empty);

        _pauseButton = new Button
        {
            AutoSize = false,
            Size = new Size(102, 34),
            Text = "暂停提醒"
        };
        _pauseButton.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);

        _settingsButton = new Button
        {
            AutoSize = false,
            Size = new Size(102, 34),
            Text = "打开设置"
        };
        _settingsButton.Click += (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 8, 10, 10),
            WrapContents = false
        };
        buttonPanel.Controls.Add(_settingsButton);
        buttonPanel.Controls.Add(_pauseButton);
        buttonPanel.Controls.Add(_snoozeButton);
        buttonPanel.Controls.Add(_acknowledgeButton);

        TableLayoutPanel bodyPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(12, 6, 12, 0),
            RowCount = 7
        };
        for (int i = 0; i < 7; i++)
        {
            bodyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        }

        bodyPanel.Controls.Add(_intervalLabel, 0, 0);
        bodyPanel.Controls.Add(_currentCycleLabel, 0, 1);
        bodyPanel.Controls.Add(_nextCycleLabel, 0, 2);
        bodyPanel.Controls.Add(_statusLabel, 0, 3);
        bodyPanel.Controls.Add(_modeLabel, 0, 4);
        bodyPanel.Controls.Add(_countdownLabel, 0, 5);
        bodyPanel.Controls.Add(_noticeLabel, 0, 6);

        _attentionTimer = new System.Windows.Forms.Timer
        {
            Interval = 450
        };
        _attentionTimer.Tick += AttentionTimer_Tick;

        Controls.Add(bodyPanel);
        Controls.Add(buttonPanel);
        Controls.Add(_titleLabel);

        ApplyEscalationPalette(1);
    }

    public event EventHandler? SaveAcknowledgedRequested;

    public event EventHandler? SnoozeRequested;

    public event EventHandler? PauseRequested;

    public event EventHandler? OpenSettingsRequested;

    public void UpdatePresentation(ReminderSnapshot snapshot, AppSettings settings)
    {
        DateTimeOffset nowLocal = DateTimeOffset.Now;

        _intervalLabel.Text = $"提醒间隔：{settings.IntervalMinutes} 分钟";
        _currentCycleLabel.Text = snapshot.CurrentReminderDueUtc.HasValue
            ? $"本次提醒：{FormatLocalTime(snapshot.CurrentReminderDueUtc.Value)}"
            : "本次提醒：--";
        _nextCycleLabel.Text = snapshot.NextReminderUtc.HasValue
            ? $"下次周期：{FormatLocalTime(snapshot.NextReminderUtc.Value)}"
            : "下次周期：暂停中";
        _statusLabel.Text = BuildStatusText(snapshot);
        _modeLabel.Text = settings.AcknowledgeResetsCycle
            ? "确认策略：确认后重新计时"
            : "确认策略：确认后保持固定周期";
        _countdownLabel.Text = snapshot.NextReminderUtc.HasValue
            ? $"距离下一周期：{FormatRemaining(snapshot.NextReminderUtc.Value.ToLocalTime() - nowLocal)}"
            : "距离下一周期：--";
        _noticeLabel.Text = snapshot.ResumedFromSnooze
            ? "状态提示：已从稍后提醒恢复，请尽快保存。"
            : $"状态提示：可稍后 {Math.Max(1, settings.DefaultSnoozeMinutes)} 分钟后再提醒。";

        UpdateAttentionState(snapshot.EscalationLevel);
        PositionWindow();
    }

    public void SetReminderInactive()
    {
        _attentionTimer.Stop();
        _attentionPulseOn = false;
        _currentEscalationLevel = 0;
        ApplyEscalationPalette(1);
    }

    public void PrepareForAppExit()
    {
        _allowClose = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _attentionTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AttentionTimer_Tick(object? sender, EventArgs e)
    {
        _attentionPulseOn = !_attentionPulseOn;
        ApplyEscalationPalette(_currentEscalationLevel);
    }

    private void UpdateAttentionState(int escalationLevel)
    {
        escalationLevel = Math.Max(1, escalationLevel);

        if (_currentEscalationLevel != escalationLevel)
        {
            _attentionPulseOn = false;
        }

        _currentEscalationLevel = escalationLevel;

        if (escalationLevel >= 3)
        {
            if (!_attentionTimer.Enabled)
            {
                _attentionTimer.Start();
            }
        }
        else
        {
            _attentionTimer.Stop();
            _attentionPulseOn = false;
        }

        ApplyEscalationPalette(escalationLevel);
    }

    private void ApplyEscalationPalette(int escalationLevel)
    {
        if (escalationLevel >= 3)
        {
            if (_attentionPulseOn)
            {
                BackColor = Color.FromArgb(255, 221, 210);
                _titleLabel.ForeColor = Color.FromArgb(140, 17, 17);
            }
            else
            {
                BackColor = Color.FromArgb(255, 238, 232);
                _titleLabel.ForeColor = Color.FromArgb(171, 28, 28);
            }

            _titleLabel.Text = "请立即保存";
            return;
        }

        if (escalationLevel == 2)
        {
            BackColor = Color.FromArgb(255, 234, 214);
            _titleLabel.ForeColor = Color.FromArgb(154, 63, 0);
            _titleLabel.Text = "还没保存";
            return;
        }

        BackColor = Color.FromArgb(255, 249, 219);
        _titleLabel.ForeColor = Color.FromArgb(182, 68, 0);
        _titleLabel.Text = "该保存了";
    }

    private static Label CreateBodyLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(45, 45, 45),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static string BuildStatusText(ReminderSnapshot snapshot)
    {
        return snapshot.OverdueCycles <= 1
            ? $"连续未确认：1 个周期 | 当前等级：Level {Math.Max(1, snapshot.EscalationLevel)}"
            : $"连续未确认：{snapshot.OverdueCycles} 个周期 | 当前等级：Level {Math.Max(1, snapshot.EscalationLevel)}";
    }

    private static string FormatLocalTime(DateTimeOffset utcTime)
    {
        DateTimeOffset localTime = utcTime.ToLocalTime();
        return localTime.Date == DateTimeOffset.Now.Date
            ? $"{localTime:HH:mm:ss}"
            : $"{localTime:MM-dd HH:mm:ss}";
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private void PositionWindow()
    {
        Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        Location = new Point(
            workingArea.Right - Width - 16,
            workingArea.Bottom - Height - 16);
    }
}
