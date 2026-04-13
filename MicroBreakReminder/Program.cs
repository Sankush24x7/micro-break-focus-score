namespace MicroBreakReminder;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.ThreadException += (_, args) => LogUnhandledException(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogUnhandledException(args.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception."));

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }

    private static void LogUnhandledException(Exception exception)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MicroBreakReminder");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "crash.log");
            File.AppendAllText(
                path,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {exception}\n------------------------------\n");
        }
        catch
        {
            // Avoid recursive failures during crash handling.
        }
    }
}
