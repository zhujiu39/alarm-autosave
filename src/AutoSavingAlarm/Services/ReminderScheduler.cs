using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.Services;

internal sealed class ReminderScheduler
{
    private readonly AppSettings _settings;
    private DateTimeOffset? _currentReminderDueUtc;

    public ReminderScheduler(AppSettings settings)
    {
        _settings = settings;
    }

    public AppSettings Settings => _settings;

    public ReminderSnapshot Evaluate(DateTimeOffset nowUtc)
    {
        if (_settings.IsPaused)
        {
            _currentReminderDueUtc = null;
            return new ReminderSnapshot(ReminderState.Paused, false, null, null);
        }

        if (_currentReminderDueUtc.HasValue)
        {
            return CreateSnapshot(ReminderState.Reminder, false, _currentReminderDueUtc.Value, nowUtc);
        }

        DateTimeOffset? latestDueUtc = GetLatestDueOnOrBefore(nowUtc);
        DateTimeOffset? acknowledgedUtc = GetEffectiveAcknowledgedTimeUtc();

        if (latestDueUtc.HasValue &&
            (!acknowledgedUtc.HasValue || acknowledgedUtc.Value < latestDueUtc.Value))
        {
            _currentReminderDueUtc = latestDueUtc;
            return CreateSnapshot(ReminderState.Reminder, true, _currentReminderDueUtc.Value, nowUtc);
        }

        return CreateSnapshot(ReminderState.Normal, false, null, nowUtc);
    }

    public void ResetAnchor(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;
        _settings.AnchorTimeUtc = nowUtc;
        _settings.LastAcknowledgedAtUtc = null;
        _currentReminderDueUtc = null;
    }

    public void Pause()
    {
        _settings.IsPaused = true;
        _currentReminderDueUtc = null;
    }

    public void Resume(DateTimeOffset nowUtc)
    {
        _settings.IsPaused = false;

        if (_settings.ResumePolicy == ResumePolicy.ResetOnResume)
        {
            ResetAnchor(nowUtc);
        }

        _currentReminderDueUtc = null;
    }

    public void Acknowledge(DateTimeOffset nowUtc)
    {
        _settings.LastAcknowledgedAtUtc = nowUtc;
        _currentReminderDueUtc = null;
    }

    private ReminderSnapshot CreateSnapshot(
        ReminderState state,
        bool reminderTriggered,
        DateTimeOffset? currentReminderDueUtc,
        DateTimeOffset nowUtc)
    {
        return new ReminderSnapshot(
            state,
            reminderTriggered,
            currentReminderDueUtc,
            GetFirstDueAfter(nowUtc));
    }

    private DateTimeOffset? GetEffectiveAcknowledgedTimeUtc()
    {
        if (!_settings.LastAcknowledgedAtUtc.HasValue)
        {
            return null;
        }

        if (_settings.LastAcknowledgedAtUtc.Value <= _settings.AnchorTimeUtc)
        {
            return null;
        }

        return _settings.LastAcknowledgedAtUtc.Value;
    }

    private DateTimeOffset? GetLatestDueOnOrBefore(DateTimeOffset nowUtc)
    {
        TimeSpan interval = GetInterval();
        DateTimeOffset firstDueUtc = _settings.AnchorTimeUtc.Add(interval);

        if (nowUtc < firstDueUtc)
        {
            return null;
        }

        long elapsedTicks = (nowUtc - firstDueUtc).Ticks;
        long intervalsPassed = elapsedTicks / interval.Ticks;

        return firstDueUtc.AddTicks(intervalsPassed * interval.Ticks);
    }

    private DateTimeOffset GetFirstDueAfter(DateTimeOffset nowUtc)
    {
        TimeSpan interval = GetInterval();
        DateTimeOffset firstDueUtc = _settings.AnchorTimeUtc.Add(interval);

        if (nowUtc < firstDueUtc)
        {
            return firstDueUtc;
        }

        long elapsedTicks = (nowUtc - firstDueUtc).Ticks;
        long intervalsPassed = elapsedTicks / interval.Ticks;

        return firstDueUtc.AddTicks((intervalsPassed + 1) * interval.Ticks);
    }

    private TimeSpan GetInterval()
    {
        return TimeSpan.FromMinutes(Math.Max(1, _settings.IntervalMinutes));
    }
}
