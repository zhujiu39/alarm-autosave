using AutoSavingAlarm.Configuration;
using AutoSavingAlarm.Services;

namespace AutoSavingAlarm.UI;

internal sealed class ReminderWindow : Form
{
    private readonly Label _titleLabel;
    private readonly Label _intervalLabel;
    private readonly Label _currentCycleLabel;
    private readonly Label _nextCycleLabel;
    private readonly Label _countdownLabel;
    private readonly Button _acknowledgeButton;
    private readonly Button _pauseButton;
    private readonly Button _settingsButton;
    private bool _allowClose;

    public ReminderWindow()
    {
        Text = "AutoSavingAlarm";
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ClientSize = new Size(360, 210);
        BackColor = Color.FromArgb(255, 249, 219);

        _titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(182, 68, 0),
            Padding = new Padding(14, 8, 14, 0),
            Text = "该保存了"
        };

        _intervalLabel = CreateBodyLabel();
        _currentCycleLabel = CreateBodyLabel();
        _nextCycleLabel = CreateBodyLabel();
        _countdownLabel = CreateBodyLabel();

        _acknowledgeButton = new Button
        {
            AutoSize = true,
            Text = "我刚保存了"
        };
        _acknowledgeButton.Click += (_, _) => SaveAcknowledgedRequested?.Invoke(this, EventArgs.Empty);

        _pauseButton = new Button
        {
            AutoSize = true,
            Text = "暂停提醒"
        };
        _pauseButton.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);

        _settingsButton = new Button
        {
            AutoSize = true,
            Text = "打开设置"
        };
        _settingsButton.Click += (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 8, 10, 10),
            WrapContents = false
        };
        buttonPanel.Controls.Add(_settingsButton);
        buttonPanel.Controls.Add(_pauseButton);
        buttonPanel.Controls.Add(_acknowledgeButton);

        TableLayoutPanel bodyPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(12, 6, 12, 0),
            RowCount = 4
        };
        bodyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        bodyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        bodyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        bodyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        bodyPanel.Controls.Add(_intervalLabel, 0, 0);
        bodyPanel.Controls.Add(_currentCycleLabel, 0, 1);
        bodyPanel.Controls.Add(_nextCycleLabel, 0, 2);
        bodyPanel.Controls.Add(_countdownLabel, 0, 3);

        Controls.Add(bodyPanel);
        Controls.Add(buttonPanel);
        Controls.Add(_titleLabel);
    }

    public event EventHandler? SaveAcknowledgedRequested;

    public event EventHandler? PauseRequested;

    public event EventHandler? OpenSettingsRequested;

    public void UpdatePresentation(ReminderSnapshot snapshot, AppSettings settings)
    {
        DateTimeOffset nowLocal = DateTimeOffset.Now;

        _intervalLabel.Text = $"提醒间隔：{settings.IntervalMinutes} 分钟";
        _currentCycleLabel.Text = snapshot.CurrentReminderDueUtc.HasValue
            ? $"当前周期：{FormatLocalTime(snapshot.CurrentReminderDueUtc.Value)}"
            : "当前周期：--";
        _nextCycleLabel.Text = snapshot.NextReminderUtc.HasValue
            ? $"下一周期：{FormatLocalTime(snapshot.NextReminderUtc.Value)}"
            : "下一周期：暂停中";
        _countdownLabel.Text = snapshot.NextReminderUtc.HasValue
            ? $"距离下一周期：{FormatRemaining(snapshot.NextReminderUtc.Value.ToLocalTime() - nowLocal)}"
            : "距离下一周期：--";

        PositionWindow();
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

    private static Label CreateBodyLabel()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(45, 45, 45),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static string FormatLocalTime(DateTimeOffset utcTime)
    {
        DateTimeOffset localTime = utcTime.ToLocalTime();
        return $"{localTime:yyyy-MM-dd HH:mm:ss}";
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
