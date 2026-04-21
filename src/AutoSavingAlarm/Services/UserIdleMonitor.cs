using AutoSavingAlarm.Configuration;
using System.Runtime.InteropServices;

namespace AutoSavingAlarm.Services;

internal sealed class UserIdleMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    public bool IsIdle(AppSettings settings)
    {
        if (!settings.IdleDetectionEnabled)
        {
            return false;
        }

        return GetIdleDuration() >= TimeSpan.FromMinutes(Math.Max(1, settings.IdleThresholdMinutes));
    }

    private static TimeSpan GetIdleDuration()
    {
        LastInputInfo lastInputInfo = new()
        {
            cbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref lastInputInfo))
        {
            return TimeSpan.Zero;
        }

        uint tickCount = unchecked((uint)Environment.TickCount);
        uint idleMilliseconds = tickCount - lastInputInfo.dwTime;
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }
}
