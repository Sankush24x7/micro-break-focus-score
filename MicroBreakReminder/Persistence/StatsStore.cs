using System.Text.Json;
using System.Text;
using MicroBreakReminder.Models;

namespace MicroBreakReminder.Persistence;

internal sealed class StatsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dataFolder;

    public StatsStore()
    {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MicroBreakReminder");
        Directory.CreateDirectory(_dataFolder);
    }

    public DailyStats LoadTodayOrCreate()
    {
        var path = GetFilePath(DateTime.Today);
        if (!File.Exists(path))
        {
            return new DailyStats { DateLocal = DateTime.Today };
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<DailyStats>(json, SerializerOptions);
            if (loaded is null || loaded.DateLocal.Date != DateTime.Today)
            {
                return new DailyStats { DateLocal = DateTime.Today };
            }

            Normalize(loaded);
            return loaded;
        }
        catch
        {
            return new DailyStats { DateLocal = DateTime.Today };
        }
    }

    public void Save(DailyStats stats)
    {
        var path = GetFilePath(stats.DateLocal.Date);
        var json = JsonSerializer.Serialize(stats, SerializerOptions);
        File.WriteAllText(path, json);
    }

    public IReadOnlyList<DailyStats> LoadRecentDays(int days)
    {
        var result = new List<DailyStats>();
        var start = DateTime.Today.AddDays(-(days - 1));
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i).Date;
            var path = GetFilePath(date);
            if (!File.Exists(path))
            {
                result.Add(new DailyStats { DateLocal = date });
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<DailyStats>(json, SerializerOptions);
                if (loaded is null)
                {
                    result.Add(new DailyStats { DateLocal = date });
                }
                else
                {
                    Normalize(loaded);
                    result.Add(loaded);
                }
            }
            catch
            {
                result.Add(new DailyStats { DateLocal = date });
            }
        }

        return result;
    }

    public string ExportRecentDaysCsv(int days, string outputPath)
    {
        var rows = LoadRecentDays(days);
        var sb = new StringBuilder();
        sb.AppendLine("Date,FocusScore,FocusHours,ActiveHours,BreaksTaken,BreaksDue,Interruptions");

        foreach (var row in rows)
        {
            var focusHours = TimeSpan.FromSeconds(row.FocusSeconds).TotalHours;
            var activeHours = TimeSpan.FromSeconds(row.ActiveSeconds).TotalHours;
            sb.AppendLine(
                $"{row.DateLocal:yyyy-MM-dd}," +
                $"{row.FocusScore}," +
                $"{focusHours:F2}," +
                $"{activeHours:F2}," +
                $"{row.BreaksTaken}," +
                $"{row.BreaksDue}," +
                $"{row.Interruptions}");
        }

        File.WriteAllText(outputPath, sb.ToString());
        return outputPath;
    }

    private string GetFilePath(DateTime dateLocal)
    {
        return Path.Combine(_dataFolder, $"stats-{dateLocal:yyyy-MM-dd}.json");
    }

    private static void Normalize(DailyStats stats)
    {
        if (stats.HourlyActiveSeconds is null || stats.HourlyActiveSeconds.Length != 24)
        {
            stats.HourlyActiveSeconds = new double[24];
        }

        if (stats.HourlyFocusSeconds is null || stats.HourlyFocusSeconds.Length != 24)
        {
            stats.HourlyFocusSeconds = new double[24];
        }

        if (stats.MinuteStates is null || stats.MinuteStates.Length != 24 * 60)
        {
            stats.MinuteStates = new int[24 * 60];
        }
    }
}
