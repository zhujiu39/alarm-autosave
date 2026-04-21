using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.Services;

internal sealed class ReminderScheduler
{
    private const int MaximumEscalationLevel = 3;

    private readonly AppSettings _settings;
    private bool _wasReminderActive;
    private int _lastEscalationLevel;

    public ReminderScheduler(AppSettings settings)
    {
        _settings = settings;
    }

    public AppSettings Settings => _settings;

    public ReminderSnapshot Evaluate(DateTimeOffset nowUtc)
    {
        if (_settings.IsPaused)
        {
            ResetRuntimeState();
            return new ReminderSnapshot(ReminderState.Paused, false, null, null, 0, 0, false);
        }

        TimeSpan interval = GetInterval();
        int completedCycleCount = GetCompletedCycleCount(nowUtc, interval);
        DateTimeOffset nextReminderUtc = GetNextReminderUtc(interval, completedCycleCount);
        int acknowledgedCycleCount = GetAcknowledgedCycleCount(interval, completedCycleCount);
        int overdueCycles = Math.Max(0, completedCycleCount - acknowledgedCycleCount);

        if (overdueCycles <= 0)
        {
            ResetRuntimeState();
            return new ReminderSnapshot(ReminderState.Normal, false, null, nextReminderUtc, 0, 0, false);
        }

        int escalationLevel = Math.Min(overdueCycles, MaximumEscalationLevel);
        bool reminderTriggered = !_wasReminderActive;
        bool escalationAdvanced = _wasReminderActive && escalationLevel > _lastEscalationLevel;
        DateTimeOffset currentReminderDueUtc = _settings.AnchorTimeUtc.AddTicks(completedCycleCount * interval.Ticks);

        _wasReminderActive = true;
        _lastEscalationLevel = escalationLevel;

        return new ReminderSnapshot(
            ReminderState.Reminder,
            reminderTriggered,
            currentReminderDueUtc,
            nextReminderUtc,
            overdueCycles,
            escalationLevel,
            escalationAdvanced);
    }

    public void ResetAnchor(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;
        _settings.AnchorTimeUtc = nowUtc;
        _settings.LastAcknowledgedAtUtc = null;
        ResetRuntimeState();
    }

    public void Pause()
    {
        _settings.IsPaused = true;
        ResetRuntimeState();
    }

    public void Resume(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;

        if (_settings.ResumePolicy == ResumePolicy.ResetOnResume)
        {
            ResetAnchor(nowUtc);
            return;
        }

        ResetRuntimeState();
    }

    public void Acknowledge(DateTimeOffset nowUtc)
    {
        if (_settings.AcknowledgeResetsCycle)
        {
            _settings.AnchorTimeUtc = nowUtc;
        }

        _settings.LastAcknowledgedAtUtc = nowUtc;
        ResetRuntimeState();
    }

    private int GetAcknowledgedCycleCount(TimeSpan interval, int completedCycleCount)
    {
        if (!_settings.LastAcknowledgedAtUtc.HasValue)
        {
            return 0;
        }

        if (_settings.LastAcknowledgedAtUtc.Value <= _settings.AnchorTimeUtc)
        {
            return 0;
        }

        long elapsedTicks = (_settings.LastAcknowledgedAtUtc.Value - _settings.AnchorTimeUtc).Ticks;
        int acknowledgedCycleCount = (int)(elapsedTicks / interval.Ticks);
        return Math.Min(Math.Max(0, acknowledgedCycleCount), completedCycleCount);
    }

    private int GetCompletedCycleCount(DateTimeOffset nowUtc, TimeSpan interval)
    {
        if (nowUtc <= _settings.AnchorTimeUtc)
        {
            return 0;
        }

        long elapsedTicks = (nowUtc - _settings.AnchorTimeUtc).Ticks;
        int completedCycleCount = (int)(elapsedTicks / interval.Ticks);
        return Math.Max(0, completedCycleCount);
    }

    private DateTimeOffset GetNextReminderUtc(TimeSpan interval, int completedCycleCount)
    {
        return _settings.AnchorTimeUtc.AddTicks((completedCycleCount + 1L) * interval.Ticks);
    }

    private TimeSpan GetInterval()
    {
        return TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
    }

    private void ResetRuntimeState()
    {
        _wasReminderActive = false;
        _lastEscalationLevel = 0;
    }
}
