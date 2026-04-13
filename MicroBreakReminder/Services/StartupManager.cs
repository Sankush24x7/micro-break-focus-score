using Microsoft.Win32;

namespace MicroBreakReminder.Services;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MicroBreakReminder";

    public static bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return runKey?.GetValue(AppName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            runKey.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            runKey.DeleteValue(AppName, false);
        }
    }
}
