namespace AutoSavingAlarm.Configuration;

internal sealed class AppSettings
{
    public int IntervalMinutes { get; set; } = 15;

    public bool StartWithWindows { get; set; }

    public bool IsPaused { get; set; }

    public bool AcknowledgeResetsCycle { get; set; } = true;

    public bool SoundEnabled { get; set; }

    public DateTimeOffset AnchorTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastAcknowledgedAtUtc { get; set; }

    public ResumePolicy ResumePolicy { get; set; } = ResumePolicy.ResetOnResume;

    public static AppSettings CreateDefault(DateTimeOffset nowUtc)
    {
        return new AppSettings
        {
            AnchorTimeUtc = nowUtc,
            AcknowledgeResetsCycle = true,
            ResumePolicy = ResumePolicy.ResetOnResume
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
            AnchorTimeUtc = AnchorTimeUtc,
            LastAcknowledgedAtUtc = LastAcknowledgedAtUtc,
            ResumePolicy = ResumePolicy
        };
    }

    public AppSettings Sanitize(DateTimeOffset nowUtc)
    {
        AppSettings sanitized = Clone();

        if (sanitized.IntervalMinutes < 1)
        {
            sanitized.IntervalMinutes = 1;
        }

        if (sanitized.AnchorTimeUtc == default)
        {
            sanitized.AnchorTimeUtc = nowUtc;
        }

        if (sanitized.LastAcknowledgedAtUtc == default)
        {
            sanitized.LastAcknowledgedAtUtc = null;
        }

        if (!Enum.IsDefined(sanitized.ResumePolicy))
        {
            sanitized.ResumePolicy = ResumePolicy.ResetOnResume;
        }

        return sanitized;
    }
}
