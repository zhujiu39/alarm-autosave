namespace AutoSavingAlarm.Services;

internal enum ReminderState
{
    Normal,
    Reminder,
    Paused
}

internal sealed record ReminderSnapshot(
    ReminderState State,
    bool ReminderTriggered,
    DateTimeOffset? CurrentReminderDueUtc,
    DateTimeOffset? NextReminderUtc,
    int OverdueCycles,
    int EscalationLevel,
    bool EscalationAdvanced);
