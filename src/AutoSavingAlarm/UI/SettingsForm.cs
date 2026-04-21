using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.UI;

internal sealed class SettingsForm : Form
{
    private readonly NumericUpDown _intervalNumericUpDown;
    private readonly CheckBox _acknowledgeResetsCycleCheckBox;
    private readonly CheckBox _soundEnabledCheckBox;
    private readonly CheckBox _startWithWindowsCheckBox;
    private readonly CheckBox _pausedCheckBox;
    private readonly ComboBox _resumePolicyComboBox;
    private readonly AppSettings _sourceSettings;

    public SettingsForm(AppSettings currentSettings)
    {
        _sourceSettings = currentSettings.Clone();

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
        MinimumSize = new Size(500, 320);
        Padding = new Padding(16, 16, 16, 16);

        Label introLabel = new()
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular),
            MaximumSize = new Size(620, 0),
            Margin = new Padding(0, 0, 0, 14),
            Text = "设置提醒间隔、确认策略和可选声音提示。修改提醒间隔后会从当前时刻重新开始计时。"
        };

        Label intervalLabel = CreateRowLabel("提醒间隔（分钟）");
        _intervalNumericUpDown = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 720,
            Value = Math.Max(1, _sourceSettings.IntervalMinutes),
            Width = 140,
            Anchor = AnchorStyles.Left
        };

        _acknowledgeResetsCycleCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "确认后重新计时（点击“我已保存”后重置周期）",
            Checked = _sourceSettings.AcknowledgeResetsCycle
        };

        _soundEnabledCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "启用声音提示（升级提醒时播放系统提示音）",
            Checked = _sourceSettings.SoundEnabled
        };

        Label resumePolicyLabel = CreateRowLabel("恢复策略");
        _resumePolicyComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        _resumePolicyComboBox.Items.Add(new ResumePolicyOption(ResumePolicy.ResetOnResume, "恢复即重置"));
        _resumePolicyComboBox.Items.Add(new ResumePolicyOption(ResumePolicy.KeepAnchorOnResume, "沿用旧锚点"));
        _resumePolicyComboBox.SelectedItem = _resumePolicyComboBox.Items
            .Cast<ResumePolicyOption>()
            .First(item => item.Policy == _sourceSettings.ResumePolicy);

        _startWithWindowsCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "开机自动启动",
            Checked = _sourceSettings.StartWithWindows
        };

        _pausedCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "保存后保持暂停",
            Checked = _sourceSettings.IsPaused
        };

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

        TableLayoutPanel contentTable = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = Padding.Empty,
            RowCount = 6
        };
        contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        contentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentTable.Controls.Add(intervalLabel, 0, 0);
        contentTable.Controls.Add(_intervalNumericUpDown, 1, 0);
        contentTable.Controls.Add(_acknowledgeResetsCycleCheckBox, 1, 1);
        contentTable.Controls.Add(_soundEnabledCheckBox, 1, 2);
        contentTable.Controls.Add(resumePolicyLabel, 0, 3);
        contentTable.Controls.Add(_resumePolicyComboBox, 1, 3);
        contentTable.Controls.Add(_startWithWindowsCheckBox, 1, 4);
        contentTable.Controls.Add(_pausedCheckBox, 1, 5);

        TableLayoutPanel rootLayout = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.Controls.Add(introLabel, 0, 0);
        rootLayout.Controls.Add(contentTable, 0, 1);
        rootLayout.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(rootLayout);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    public AppSettings? SubmittedSettings { get; private set; }

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

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        ResumePolicyOption selectedPolicy = (ResumePolicyOption)_resumePolicyComboBox.SelectedItem!;

        SubmittedSettings = new AppSettings
        {
            IntervalMinutes = Decimal.ToInt32(_intervalNumericUpDown.Value),
            AcknowledgeResetsCycle = _acknowledgeResetsCycleCheckBox.Checked,
            SoundEnabled = _soundEnabledCheckBox.Checked,
            StartWithWindows = _startWithWindowsCheckBox.Checked,
            IsPaused = _pausedCheckBox.Checked,
            AnchorTimeUtc = _sourceSettings.AnchorTimeUtc,
            LastAcknowledgedAtUtc = _sourceSettings.LastAcknowledgedAtUtc,
            ResumePolicy = selectedPolicy.Policy
        };

        DialogResult = DialogResult.OK;
        Close();
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
