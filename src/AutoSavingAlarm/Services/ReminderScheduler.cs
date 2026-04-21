using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.Services;

internal sealed class ReminderScheduler
{
    private const int MaximumEscalationLevel = 3;

    private readonly AppSettings _settings;
    private readonly WorkScheduleEvaluator _workScheduleEvaluator;
    private ReminderState _lastState;
    private int _lastEscalationLevel;

    public ReminderScheduler(AppSettings settings, WorkScheduleEvaluator workScheduleEvaluator)
    {
        _settings = settings;
        _workScheduleEvaluator = workScheduleEvaluator;
        _lastState = settings.IsPaused ? ReminderState.Paused : ReminderState.Normal;
    }

    public AppSettings Settings => _settings;

    public ReminderSnapshot Evaluate(DateTimeOffset nowUtc, bool isIdle)
    {
        bool settingsChanged = false;
        bool resumedFromSnooze = false;
        DateTimeOffset nowLocal = nowUtc.ToLocalTime();
        ReminderState previousState = _lastState;
        int previousEscalationLevel = _lastEscalationLevel;
        bool isWithinWorkSchedule = _workScheduleEvaluator.IsWithinWorkSchedule(_settings, nowLocal);

        if (_settings.IsPaused)
        {
            return CreateSnapshot(
                ReminderState.Paused,
                settingsChanged,
                reminderTriggered: false,
                currentReminderDueUtc: null,
                nextReminderUtc: null,
                overdueCycles: 0,
                escalationLevel: 0,
                escalationAdvanced: false,
                snoozeUntilUtc: _settings.SnoozeUntilUtc,
                isWithinWorkSchedule,
                isIdleSuspended: false,
                resumedFromSnooze: false);
        }

        if (!isWithinWorkSchedule)
        {
            settingsChanged |= ClearSnooze();
            return CreateSnapshot(
                ReminderState.SuppressedBySchedule,
                settingsChanged,
                reminderTriggered: false,
                currentReminderDueUtc: null,
                nextReminderUtc: null,
                overdueCycles: 0,
                escalationLevel: 0,
                escalationAdvanced: false,
                snoozeUntilUtc: _settings.SnoozeUntilUtc,
                isWithinWorkSchedule,
                isIdleSuspended: false,
                resumedFromSnooze: false);
        }

        if (previousState == ReminderState.SuppressedBySchedule)
        {
            ResetAnchorInternal(nowUtc, clearAcknowledgement: true);
            settingsChanged = true;
        }

        bool isIdleSuspended = _settings.IdleDetectionEnabled && isIdle;
        if (isIdleSuspended)
        {
            settingsChanged |= ClearSnooze();
            return CreateSnapshot(
                ReminderState.SuppressedByIdle,
                settingsChanged,
                reminderTriggered: false,
                currentReminderDueUtc: null,
                nextReminderUtc: null,
                overdueCycles: 0,
                escalationLevel: 0,
                escalationAdvanced: false,
                snoozeUntilUtc: _settings.SnoozeUntilUtc,
                isWithinWorkSchedule,
                isIdleSuspended: true,
                resumedFromSnooze: false);
        }

        if (previousState == ReminderState.SuppressedByIdle)
        {
            ResetAnchorInternal(nowUtc, clearAcknowledgement: true);
            settingsChanged = true;
        }

        if (_settings.SnoozeUntilUtc.HasValue)
        {
            if (_settings.SnoozeUntilUtc.Value > nowUtc)
            {
                return CreateSnapshot(
                    ReminderState.Snoozed,
                    settingsChanged,
                    reminderTriggered: false,
                    currentReminderDueUtc: null,
                    nextReminderUtc: GetNextReminderUtc(nowUtc),
                    overdueCycles: 0,
                    escalationLevel: 0,
                    escalationAdvanced: false,
                    snoozeUntilUtc: _settings.SnoozeUntilUtc,
                    isWithinWorkSchedule,
                    isIdleSuspended: false,
                    resumedFromSnooze: false);
            }

            _settings.SnoozeUntilUtc = null;
            settingsChanged = true;
            resumedFromSnooze = previousState == ReminderState.Snoozed;
        }

        TimeSpan interval = GetInterval();
        int completedCycleCount = GetCompletedCycleCount(nowUtc, interval);
        DateTimeOffset nextReminderUtc = GetNextReminderUtc(interval, completedCycleCount);
        int acknowledgedCycleCount = GetAcknowledgedCycleCount(interval, completedCycleCount);
        int overdueCycles = Math.Max(0, completedCycleCount - acknowledgedCycleCount);

        if (overdueCycles <= 0)
        {
            return CreateSnapshot(
                ReminderState.Normal,
                settingsChanged,
                reminderTriggered: false,
                currentReminderDueUtc: null,
                nextReminderUtc,
                overdueCycles: 0,
                escalationLevel: 0,
                escalationAdvanced: false,
                snoozeUntilUtc: null,
                isWithinWorkSchedule,
                isIdleSuspended: false,
                resumedFromSnooze: resumedFromSnooze);
        }

        int escalationLevel = Math.Min(overdueCycles, MaximumEscalationLevel);
        bool reminderTriggered = previousState != ReminderState.Reminder;
        bool escalationAdvanced = previousState == ReminderState.Reminder && escalationLevel > previousEscalationLevel;
        DateTimeOffset currentReminderDueUtc = _settings.AnchorTimeUtc.AddTicks(completedCycleCount * interval.Ticks);

        return CreateSnapshot(
            ReminderState.Reminder,
            settingsChanged,
            reminderTriggered,
            currentReminderDueUtc,
            nextReminderUtc,
            overdueCycles,
            escalationLevel,
            escalationAdvanced,
            snoozeUntilUtc: null,
            isWithinWorkSchedule,
            isIdleSuspended: false,
            resumedFromSnooze: resumedFromSnooze);
    }

    public void ResetAnchor(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;
        ResetAnchorInternal(nowUtc, clearAcknowledgement: true);
    }

    public void Pause()
    {
        _settings.IsPaused = true;
        ClearSnooze();
    }

    public void Resume(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;
        ClearSnooze();

        if (_settings.ResumePolicy == ResumePolicy.ResetOnResume)
        {
            ResetAnchorInternal(nowUtc, clearAcknowledgement: true);
        }
    }

    public void Acknowledge(DateTimeOffset nowUtc)
    {
        ClearSnooze();

        if (_settings.AcknowledgeResetsCycle)
        {
            _settings.AnchorTimeUtc = nowUtc;
        }

        _settings.LastAcknowledgedAtUtc = nowUtc;
    }

    public void Snooze(DateTimeOffset nowUtc, int minutes)
    {
        int sanitizedMinutes = Math.Max(1, minutes);
        _settings.SnoozeUntilUtc = nowUtc.AddMinutes(sanitizedMinutes);
    }

    private ReminderSnapshot CreateSnapshot(
        ReminderState state,
        bool settingsChanged,
        bool reminderTriggered,
        DateTimeOffset? currentReminderDueUtc,
        DateTimeOffset? nextReminderUtc,
        int overdueCycles,
        int escalationLevel,
        bool escalationAdvanced,
        DateTimeOffset? snoozeUntilUtc,
        bool isWithinWorkSchedule,
        bool isIdleSuspended,
        bool resumedFromSnooze)
    {
        bool stateChanged = state != _lastState;

        ReminderSnapshot snapshot = new(
            state,
            stateChanged,
            settingsChanged,
            reminderTriggered,
            currentReminderDueUtc,
            nextReminderUtc,
            overdueCycles,
            escalationLevel,
            escalationAdvanced,
            snoozeUntilUtc,
            isWithinWorkSchedule,
            isIdleSuspended,
            resumedFromSnooze);

        _lastState = state;
        _lastEscalationLevel = state == ReminderState.Reminder ? escalationLevel : 0;
        return snapshot;
    }

    private bool ClearSnooze()
    {
        if (!_settings.SnoozeUntilUtc.HasValue)
        {
            return false;
        }

        _settings.SnoozeUntilUtc = null;
        return true;
    }

    private void ResetAnchorInternal(DateTimeOffset nowUtc, bool clearAcknowledgement)
    {
        _settings.AnchorTimeUtc = nowUtc;
        _settings.SnoozeUntilUtc = null;

        if (clearAcknowledgement)
        {
            _settings.LastAcknowledgedAtUtc = null;
        }
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

    private DateTimeOffset GetNextReminderUtc(DateTimeOffset nowUtc)
    {
        TimeSpan interval = GetInterval();
        int completedCycleCount = GetCompletedCycleCount(nowUtc, interval);
        return GetNextReminderUtc(interval, completedCycleCount);
    }

    private DateTimeOffset GetNextReminderUtc(TimeSpan interval, int completedCycleCount)
    {
        return _settings.AnchorTimeUtc.AddTicks((completedCycleCount + 1L) * interval.Ticks);
    }

    private TimeSpan GetInterval()
    {
        return TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
    }
}
