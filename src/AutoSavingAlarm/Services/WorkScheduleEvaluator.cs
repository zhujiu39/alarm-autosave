using AutoSavingAlarm.Configuration;

namespace AutoSavingAlarm.Services;

internal sealed class WorkScheduleEvaluator
{
    public bool IsWithinWorkSchedule(AppSettings settings, DateTimeOffset nowLocal)
    {
        if (!settings.WorkScheduleEnabled)
        {
            return true;
        }

        TimeSpan currentTime = nowLocal.TimeOfDay;
        TimeSpan start = settings.WorkdayStartLocalTime;
        TimeSpan end = settings.WorkdayEndLocalTime;

        if (start == end)
        {
            return WorkdaySchedule.IsEnabled(settings.WorkdayMask, nowLocal.DayOfWeek);
        }

        if (start < end)
        {
            return WorkdaySchedule.IsEnabled(settings.WorkdayMask, nowLocal.DayOfWeek) &&
                   currentTime >= start &&
                   currentTime < end;
        }

        if (currentTime >= start)
        {
            return WorkdaySchedule.IsEnabled(settings.WorkdayMask, nowLocal.DayOfWeek);
        }

        if (currentTime < end)
        {
            DayOfWeek previousDay = nowLocal.AddDays(-1).DayOfWeek;
            return WorkdaySchedule.IsEnabled(settings.WorkdayMask, previousDay);
        }

        return false;
    }
}
