namespace AutoSavingAlarm.Configuration;

internal sealed class AppSettings
{
    private static readonly TimeSpan DefaultWorkdayStart = TimeSpan.FromHours(9);
    private static readonly TimeSpan DefaultWorkdayEnd = TimeSpan.FromHours(18);

    public int IntervalMinutes { get; set; } = 15;

    public bool StartWithWindows { get; set; }

    public bool IsPaused { get; set; }

    public bool AcknowledgeResetsCycle { get; set; } = true;

    public bool SoundEnabled { get; set; }

    public int DefaultSnoozeMinutes { get; set; } = 10;

    public DateTimeOffset AnchorTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastAcknowledgedAtUtc { get; set; }

    public DateTimeOffset? SnoozeUntilUtc { get; set; }

    public ResumePolicy ResumePolicy { get; set; } = ResumePolicy.ResetOnResume;

    public bool WorkScheduleEnabled { get; set; }

    public WorkdayFlags WorkdayMask { get; set; } = WorkdayFlags.Weekdays;

    public TimeSpan WorkdayStartLocalTime { get; set; } = DefaultWorkdayStart;

    public TimeSpan WorkdayEndLocalTime { get; set; } = DefaultWorkdayEnd;

    public bool IdleDetectionEnabled { get; set; }

    public int IdleThresholdMinutes { get; set; } = 10;

    public static AppSettings CreateDefault(DateTimeOffset nowUtc)
    {
        return new AppSettings
        {
            AnchorTimeUtc = nowUtc,
            AcknowledgeResetsCycle = true,
            ResumePolicy = ResumePolicy.ResetOnResume,
            WorkdayMask = WorkdayFlags.Weekdays,
            WorkdayStartLocalTime = DefaultWorkdayStart,
            WorkdayEndLocalTime = DefaultWorkdayEnd
        };
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            IntervalMinutes = IntervalMinutes,
            StartWithWindows = StartWithWindows,
            IsPaused = IsPaused,
            AcknowledgeResetsCycle = AcknowledgeResetsCycle,
            SoundEnabled = SoundEnabled,
            DefaultSnoozeMinutes = DefaultSnoozeMinutes,
            AnchorTimeUtc = AnchorTimeUtc,
            LastAcknowledgedAtUtc = LastAcknowledgedAtUtc,
            SnoozeUntilUtc = SnoozeUntilUtc,
            ResumePolicy = ResumePolicy,
            WorkScheduleEnabled = WorkScheduleEnabled,
            WorkdayMask = WorkdayMask,
            WorkdayStartLocalTime = WorkdayStartLocalTime,
            WorkdayEndLocalTime = WorkdayEndLocalTime,
            IdleDetectionEnabled = IdleDetectionEnabled,
            IdleThresholdMinutes = IdleThresholdMinutes
        };
    }

    public AppSettings Sanitize(DateTimeOffset nowUtc)
    {
        AppSettings sanitized = Clone();

        if (sanitized.IntervalMinutes < 1)
        {
            sanitized.IntervalMinutes = 1;
        }

        if (sanitized.DefaultSnoozeMinutes < 1)
        {
            sanitized.DefaultSnoozeMinutes = 10;
        }

        if (sanitized.IdleThresholdMinutes < 1)
        {
            sanitized.IdleThresholdMinutes = 10;
        }

        if (sanitized.AnchorTimeUtc == default)
        {
            sanitized.AnchorTimeUtc = nowUtc;
        }

        if (sanitized.LastAcknowledgedAtUtc == default)
        {
            sanitized.LastAcknowledgedAtUtc = null;
        }

        if (sanitized.SnoozeUntilUtc == default)
        {
            sanitized.SnoozeUntilUtc = null;
        }

        if (!Enum.IsDefined(sanitized.ResumePolicy))
        {
            sanitized.ResumePolicy = ResumePolicy.ResetOnResume;
        }

        if (!WorkdaySchedule.IsValidMask(sanitized.WorkdayMask) || sanitized.WorkdayMask == WorkdayFlags.None)
        {
            sanitized.WorkdayMask = WorkdayFlags.Weekdays;
        }

        if (!IsValidLocalTime(sanitized.WorkdayStartLocalTime))
        {
            sanitized.WorkdayStartLocalTime = DefaultWorkdayStart;
        }

        if (!IsValidLocalTime(sanitized.WorkdayEndLocalTime))
        {
            sanitized.WorkdayEndLocalTime = DefaultWorkdayEnd;
        }

        return sanitized;
    }

    private static bool IsValidLocalTime(TimeSpan value)
    {
        return value >= TimeSpan.Zero && value < TimeSpan.FromDays(1);
    }
}
