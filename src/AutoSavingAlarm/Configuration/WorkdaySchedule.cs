namespace AutoSavingAlarm.Configuration;

[Flags]
internal enum WorkdayFlags
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

internal static class WorkdaySchedule
{
    public static bool IsEnabled(WorkdayFlags mask, DayOfWeek dayOfWeek)
    {
        WorkdayFlags flag = ToFlag(dayOfWeek);
        return (mask & flag) == flag;
    }

    public static bool IsValidMask(WorkdayFlags mask)
    {
        return (mask & ~WorkdayFlags.All) == 0;
    }

    public static WorkdayFlags ToFlag(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => WorkdayFlags.Sunday,
            DayOfWeek.Monday => WorkdayFlags.Monday,
            DayOfWeek.Tuesday => WorkdayFlags.Tuesday,
            DayOfWeek.Wednesday => WorkdayFlags.Wednesday,
            DayOfWeek.Thursday => WorkdayFlags.Thursday,
            DayOfWeek.Friday => WorkdayFlags.Friday,
            DayOfWeek.Saturday => WorkdayFlags.Saturday,
            _ => WorkdayFlags.None
        };
    }
}
