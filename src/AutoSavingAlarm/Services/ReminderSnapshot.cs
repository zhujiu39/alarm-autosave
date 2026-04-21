namespace AutoSavingAlarm.Services;

internal enum ReminderState
{
    Normal,
    Reminder,
    Paused,
    Snoozed,
    SuppressedBySchedule,
    SuppressedByIdle
}

internal sealed record ReminderSnapshot(
    ReminderState State,
    bool StateChanged,
    bool SettingsChanged,
    bool ReminderTriggered,
    DateTimeOffset? CurrentReminderDueUtc,
    DateTimeOffset? NextReminderUtc,
    int OverdueCycles,
    int EscalationLevel,
    bool EscalationAdvanced,
    DateTimeOffset? SnoozeUntilUtc,
    bool IsWithinWorkSchedule,
    bool IsIdleSuspended,
    bool ResumedFromSnooze);
