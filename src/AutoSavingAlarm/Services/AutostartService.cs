using Microsoft.Win32;

namespace AutoSavingAlarm.Services;

internal sealed class AutostartService : IDisposable
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AutoSavingAlarm";

    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        string? currentValue = key?.GetValue(ValueName) as string;
        return !string.IsNullOrWhiteSpace(currentValue);
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public void Dispose()
    {
    }
}
