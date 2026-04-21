using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.UI;

internal sealed class SettingsForm : Form
{
    private readonly NumericUpDown _intervalNumericUpDown;
    private readonly CheckBox _acknowledgeResetsCycleCheckBox;
    private readonly CheckBox _soundEnabledCheckBox;
    private readonly ComboBox _defaultSnoozeComboBox;
    private readonly CheckBox _workScheduleEnabledCheckBox;
    private readonly FlowLayoutPanel _workdayPanel;
    private readonly Dictionary<DayOfWeek, CheckBox> _workdayCheckBoxes;
    private readonly DateTimePicker _workdayStartPicker;
    private readonly DateTimePicker _workdayEndPicker;
    private readonly CheckBox _idleDetectionEnabledCheckBox;
    private readonly NumericUpDown _idleThresholdNumericUpDown;
    private readonly CheckBox _startWithWindowsCheckBox;
    private readonly CheckBox _pausedCheckBox;
    private readonly ComboBox _resumePolicyComboBox;
    private readonly TextBox _settingsPathTextBox;
    private readonly Label _backupStatusLabel;
    private readonly Button _restoreBackupButton;
    private readonly SettingsStore _settingsStore;
    private AppSettings _sourceSettings;

    public SettingsForm(AppSettings currentSettings, SettingsStore settingsStore)
    {
        _sourceSettings = currentSettings.Clone();
        _settingsStore = settingsStore;
        _workdayCheckBoxes = new Dictionary<DayOfWeek, CheckBox>();

        Text = "AutoSavingAlarm 设置";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(720, 760);
        Padding = new Padding(16, 16, 16, 16);

        Label introLabel = new()
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular),
            MaximumSize = new Size(760, 0),
            Margin = new Padding(0, 0, 0, 14),
            Text = "设置提醒节奏、稍后提醒、工作时段、空闲检测和配置恢复。修改提醒节奏、工作时段或空闲检测后，会从当前时刻重新开始计时。"
        };

        GroupBox basicGroupBox = CreateGroupBox("提醒基础设置");
        TableLayoutPanel basicTable = CreateTwoColumnTable();
        _intervalNumericUpDown = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 720,
            Width = 140,
            Anchor = AnchorStyles.Left
        };
        _acknowledgeResetsCycleCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "确认后重新计时（点击“我已保存”后重置周期）"
        };
        _soundEnabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "启用声音提示（升级提醒时播放系统提示音）"
        };
        basicTable.Controls.Add(CreateRowLabel("提醒间隔（分钟）"), 0, 0);
        basicTable.Controls.Add(_intervalNumericUpDown, 1, 0);
        basicTable.Controls.Add(_acknowledgeResetsCycleCheckBox, 1, 1);
        basicTable.Controls.Add(_soundEnabledCheckBox, 1, 2);
        basicGroupBox.Controls.Add(basicTable);

        GroupBox snoozeGroupBox = CreateGroupBox("稍后提醒");
        TableLayoutPanel snoozeTable = CreateTwoColumnTable();
        _defaultSnoozeComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Anchor = AnchorStyles.Left
        };
        _defaultSnoozeComboBox.Items.AddRange([5, 10, 15, 30]);
        snoozeTable.Controls.Add(CreateRowLabel("默认稍后时长（分钟）"), 0, 0);
        snoozeTable.Controls.Add(_defaultSnoozeComboBox, 1, 0);
        snoozeGroupBox.Controls.Add(snoozeTable);

        GroupBox scheduleGroupBox = CreateGroupBox("工作节奏控制");
        TableLayoutPanel scheduleTable = CreateTwoColumnTable();
        _workScheduleEnabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "仅在工作时段内提醒"
        };
        _workScheduleEnabledCheckBox.CheckedChanged += (_, _) => UpdateConditionalControlStates();

        _workdayPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 0),
            WrapContents = true
        };

        (DayOfWeek dayOfWeek, string text)[] workdayItems =
        [
            (DayOfWeek.Monday, "周一"),
            (DayOfWeek.Tuesday, "周二"),
            (DayOfWeek.Wednesday, "周三"),
            (DayOfWeek.Thursday, "周四"),
            (DayOfWeek.Friday, "周五"),
            (DayOfWeek.Saturday, "周六"),
            (DayOfWeek.Sunday, "周日")
        ];

        foreach ((DayOfWeek dayOfWeek, string text) in workdayItems)
        {
            CheckBox checkBox = new()
            {
                AutoSize = true,
                Margin = new Padding(0, 3, 12, 3),
                Text = text
            };
            _workdayCheckBoxes[dayOfWeek] = checkBox;
            _workdayPanel.Controls.Add(checkBox);
        }

        _workdayStartPicker = CreateTimePicker();
        _workdayEndPicker = CreateTimePicker();

        _idleDetectionEnabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "检测长时间无操作并自动挂起提醒"
        };
        _idleDetectionEnabledCheckBox.CheckedChanged += (_, _) => UpdateConditionalControlStates();

        _idleThresholdNumericUpDown = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 240,
            Width = 140,
            Anchor = AnchorStyles.Left
        };

        scheduleTable.Controls.Add(_workScheduleEnabledCheckBox, 1, 0);
        scheduleTable.Controls.Add(CreateRowLabel("生效日期"), 0, 1);
        scheduleTable.Controls.Add(_workdayPanel, 1, 1);
        scheduleTable.Controls.Add(CreateRowLabel("开始时间"), 0, 2);
        scheduleTable.Controls.Add(_workdayStartPicker, 1, 2);
        scheduleTable.Controls.Add(CreateRowLabel("结束时间"), 0, 3);
        scheduleTable.Controls.Add(_workdayEndPicker, 1, 3);
        scheduleTable.Controls.Add(_idleDetectionEnabledCheckBox, 1, 4);
        scheduleTable.Controls.Add(CreateRowLabel("空闲阈值（分钟）"), 0, 5);
        scheduleTable.Controls.Add(_idleThresholdNumericUpDown, 1, 5);
        scheduleGroupBox.Controls.Add(scheduleTable);

        GroupBox startupGroupBox = CreateGroupBox("启动与恢复");
        TableLayoutPanel startupTable = CreateTwoColumnTable();
        _resumePolicyComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        _resumePolicyComboBox.Items.Add(new ResumePolicyOption(ResumePolicy.ResetOnResume, "恢复即重置"));
        _resumePolicyComboBox.Items.Add(new ResumePolicyOption(ResumePolicy.KeepAnchorOnResume, "沿用旧锚点"));

        _startWithWindowsCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "开机自动启动"
        };
        _pausedCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "保存设置后保持暂停"
        };

        startupTable.Controls.Add(CreateRowLabel("恢复策略"), 0, 0);
        startupTable.Controls.Add(_resumePolicyComboBox, 1, 0);
        startupTable.Controls.Add(_startWithWindowsCheckBox, 1, 1);
        startupTable.Controls.Add(_pausedCheckBox, 1, 2);
        startupGroupBox.Controls.Add(startupTable);

        GroupBox storageGroupBox = CreateGroupBox("配置与备份");
        TableLayoutPanel storageTable = CreateTwoColumnTable();
        _settingsPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true
        };
        _backupStatusLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "--"
        };
        _restoreBackupButton = new Button
        {
            AutoSize = true,
            Text = "恢复最近备份"
        };
        _restoreBackupButton.Click += RestoreBackupButton_Click;

        storageTable.Controls.Add(CreateRowLabel("主配置路径"), 0, 0);
        storageTable.Controls.Add(_settingsPathTextBox, 1, 0);
        storageTable.Controls.Add(CreateRowLabel("备份状态"), 0, 1);
        storageTable.Controls.Add(_backupStatusLabel, 1, 1);
        storageTable.Controls.Add(new Label { AutoSize = true }, 0, 2);
        storageTable.Controls.Add(_restoreBackupButton, 1, 2);
        storageGroupBox.Controls.Add(storageTable);

        Button saveButton = new()
        {
            AutoSize = true,
            Text = "保存"
        };
        saveButton.Click += SaveButton_Click;

        Button cancelButton = new()
        {
            AutoSize = true,
            Text = "取消"
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        FlowLayoutPanel buttonPanel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 18, 0, 0),
            WrapContents = false
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);

        TableLayoutPanel rootLayout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 7
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.Controls.Add(introLabel, 0, 0);
        rootLayout.Controls.Add(basicGroupBox, 0, 1);
        rootLayout.Controls.Add(snoozeGroupBox, 0, 2);
        rootLayout.Controls.Add(scheduleGroupBox, 0, 3);
        rootLayout.Controls.Add(startupGroupBox, 0, 4);
        rootLayout.Controls.Add(storageGroupBox, 0, 5);
        rootLayout.Controls.Add(buttonPanel, 0, 6);

        Controls.Add(rootLayout);

        ApplySettingsToControls(_sourceSettings);
        RefreshStorageInfo();

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    public AppSettings? SubmittedSettings { get; private set; }

    private static GroupBox CreateGroupBox(string text)
    {
        return new GroupBox
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 20, 12, 12),
            Margin = new Padding(0, 0, 0, 14),
            Text = text
        };
    }

    private static TableLayoutPanel CreateTwoColumnTable()
    {
        TableLayoutPanel table = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    private static Label CreateRowLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 16, 8),
            Text = text
        };
    }

    private static DateTimePicker CreateTimePicker()
    {
        return new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Width = 140
        };
    }

    private void ApplySettingsToControls(AppSettings settings)
    {
        _intervalNumericUpDown.Value = Math.Max(1, settings.IntervalMinutes);
        _acknowledgeResetsCycleCheckBox.Checked = settings.AcknowledgeResetsCycle;
        _soundEnabledCheckBox.Checked = settings.SoundEnabled;

        if (!_defaultSnoozeComboBox.Items.Contains(settings.DefaultSnoozeMinutes))
        {
            _defaultSnoozeComboBox.Items.Add(settings.DefaultSnoozeMinutes);
        }
        _defaultSnoozeComboBox.SelectedItem = settings.DefaultSnoozeMinutes;

        _workScheduleEnabledCheckBox.Checked = settings.WorkScheduleEnabled;
        SetSelectedWorkdays(settings.WorkdayMask);
        _workdayStartPicker.Value = DateTime.Today.Add(settings.WorkdayStartLocalTime);
        _workdayEndPicker.Value = DateTime.Today.Add(settings.WorkdayEndLocalTime);

        _idleDetectionEnabledCheckBox.Checked = settings.IdleDetectionEnabled;
        _idleThresholdNumericUpDown.Value = Math.Max(1, settings.IdleThresholdMinutes);

        _startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        _pausedCheckBox.Checked = settings.IsPaused;
        _resumePolicyComboBox.SelectedItem = _resumePolicyComboBox.Items
            .Cast<ResumePolicyOption>()
            .First(item => item.Policy == settings.ResumePolicy);

        UpdateConditionalControlStates();
    }

    private void SetSelectedWorkdays(WorkdayFlags mask)
    {
        foreach (KeyValuePair<DayOfWeek, CheckBox> pair in _workdayCheckBoxes)
        {
            pair.Value.Checked = WorkdaySchedule.IsEnabled(mask, pair.Key);
        }
    }

    private void UpdateConditionalControlStates()
    {
        bool workScheduleEnabled = _workScheduleEnabledCheckBox.Checked;
        _workdayPanel.Enabled = workScheduleEnabled;
        _workdayStartPicker.Enabled = workScheduleEnabled;
        _workdayEndPicker.Enabled = workScheduleEnabled;

        bool idleDetectionEnabled = _idleDetectionEnabledCheckBox.Checked;
        _idleThresholdNumericUpDown.Enabled = idleDetectionEnabled;
    }

    private void RefreshStorageInfo()
    {
        SettingsStorageInfo storageInfo = _settingsStore.GetStorageInfo();
        _settingsPathTextBox.Text = storageInfo.SettingsPath;

        if (storageInfo.BackupExists)
        {
            string lastWriteTime = storageInfo.BackupLastWriteUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "--";
            _backupStatusLabel.Text = $"最近备份可用，最后更新时间：{lastWriteTime}";
            _restoreBackupButton.Enabled = true;
            return;
        }

        _backupStatusLabel.Text = "当前还没有可用的备份配置。";
        _restoreBackupButton.Enabled = false;
    }

    private void RestoreBackupButton_Click(object? sender, EventArgs e)
    {
        if (!_settingsStore.TryLoadBackup(out AppSettings backupSettings, out string? errorMessage))
        {
            MessageBox.Show(
                errorMessage ?? "恢复最近备份失败。",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            RefreshStorageInfo();
            return;
        }

        _sourceSettings = backupSettings.Clone();
        ApplySettingsToControls(_sourceSettings);
        RefreshStorageInfo();

        MessageBox.Show(
            "已载入最近可用备份。点击“保存”后会将该备份写回当前主配置。",
            "AutoSavingAlarm",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        WorkdayFlags selectedWorkdayMask = CollectSelectedWorkdayMask();

        if (_workScheduleEnabledCheckBox.Checked && selectedWorkdayMask == WorkdayFlags.None)
        {
            MessageBox.Show(
                "启用工作时段时，至少需要选择一个生效日期。",
                "AutoSavingAlarm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ResumePolicyOption selectedPolicy = (ResumePolicyOption)_resumePolicyComboBox.SelectedItem!;

        AppSettings submittedSettings = _sourceSettings.Clone();
        submittedSettings.IntervalMinutes = Decimal.ToInt32(_intervalNumericUpDown.Value);
        submittedSettings.AcknowledgeResetsCycle = _acknowledgeResetsCycleCheckBox.Checked;
        submittedSettings.SoundEnabled = _soundEnabledCheckBox.Checked;
        submittedSettings.DefaultSnoozeMinutes = GetSelectedSnoozeMinutes();
        submittedSettings.StartWithWindows = _startWithWindowsCheckBox.Checked;
        submittedSettings.IsPaused = _pausedCheckBox.Checked;
        submittedSettings.ResumePolicy = selectedPolicy.Policy;
        submittedSettings.WorkScheduleEnabled = _workScheduleEnabledCheckBox.Checked;
        submittedSettings.WorkdayMask = selectedWorkdayMask == WorkdayFlags.None
            ? WorkdayFlags.Weekdays
            : selectedWorkdayMask;
        submittedSettings.WorkdayStartLocalTime = _workdayStartPicker.Value.TimeOfDay;
        submittedSettings.WorkdayEndLocalTime = _workdayEndPicker.Value.TimeOfDay;
        submittedSettings.IdleDetectionEnabled = _idleDetectionEnabledCheckBox.Checked;
        submittedSettings.IdleThresholdMinutes = Decimal.ToInt32(_idleThresholdNumericUpDown.Value);

        SubmittedSettings = submittedSettings;
        DialogResult = DialogResult.OK;
        Close();
    }

    private int GetSelectedSnoozeMinutes()
    {
        return _defaultSnoozeComboBox.SelectedItem is int selectedValue
            ? selectedValue
            : 10;
    }

    private WorkdayFlags CollectSelectedWorkdayMask()
    {
        WorkdayFlags mask = WorkdayFlags.None;

        foreach (KeyValuePair<DayOfWeek, CheckBox> pair in _workdayCheckBoxes)
        {
            if (pair.Value.Checked)
            {
                mask |= WorkdaySchedule.ToFlag(pair.Key);
            }
        }

        return mask;
    }

    private sealed class ResumePolicyOption
    {
        public ResumePolicyOption(ResumePolicy policy, string displayText)
        {
            Policy = policy;
            DisplayText = displayText;
        }

        public ResumePolicy Policy { get; }

        public string DisplayText { get; }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
