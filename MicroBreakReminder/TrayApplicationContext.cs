using MicroBreakReminder.Persistence;
using MicroBreakReminder.Services;
using MicroBreakReminder.UI;
using MicroBreakReminder.Models;

namespace MicroBreakReminder;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _soundItem;
    private readonly StatsStore _statsStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly ActivityTracker _activityTracker;
    private readonly FocusEngine _focusEngine;
    private readonly NotificationService _notificationService;
    private readonly System.Windows.Forms.Timer _saveTimer;

    private DashboardForm? _dashboard;
    private IntPtr _lastWindow;
    private int _lastBreaksTakenObserved = -1;

    public TrayApplicationContext()
    {
        _statsStore = new StatsStore();
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _focusEngine = new FocusEngine(_statsStore.LoadTodayOrCreate());
        _activityTracker = new ActivityTracker();

        var menu = new ContextMenuStrip();
        menu.Items.Add("View Dashboard", null, (_, _) => OpenDashboard());
        _pauseItem = new ToolStripMenuItem("Pause Tracking", null, (_, _) => TogglePause());
        menu.Items.Add(_pauseItem);
        menu.Items.Add("Export Weekly CSV", null, (_, _) => ExportWeeklyCsv());
        menu.Items.Add("Snooze Reminder (5 min)", null, (_, _) => _focusEngine.SnoozeBreakReminder());
        _soundItem = new ToolStripMenuItem("Sound Alerts", null, (_, _) => ToggleSound()) { Checked = true, CheckOnClick = true };
        menu.Items.Add(_soundItem);
        _startupItem = new ToolStripMenuItem("Run At Startup", null, (_, _) => ToggleStartup()) { Checked = StartupManager.IsEnabled(), CheckOnClick = true };
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _trayIcon = new NotifyIcon
        {
            Text = "Micro-Break Reminder",
            Visible = true,
            Icon = SystemIcons.Information,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => OpenDashboard();

        _notificationService = new NotificationService(_trayIcon);

        _activityTracker.UserActivity += timestamp => _focusEngine.RegisterActivity(timestamp);
        _activityTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _focusEngine.SnapshotChanged += OnSnapshotChanged;
        _focusEngine.BreakReminderRequested += OnBreakReminderRequested;
        _focusEngine.PostureReminderRequested += () => _notificationService.ShowPostureReminder();
        _focusEngine.HydrationNudgeRequested += () => _notificationService.ShowHydrationNudge();
        _notificationService.ReminderActionInvoked += OnReminderActionInvoked;
        _activityTracker.Start();

        _saveTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _saveTimer.Tick += (_, _) => PersistStats();
        _saveTimer.Start();

        _lastBreaksTakenObserved = _focusEngine.GetSnapshot().BreaksTaken;
    }

    private void TogglePause()
    {
        _focusEngine.SetPaused(!_focusEngine.IsPaused);
        _pauseItem.Text = _focusEngine.IsPaused ? "Resume Tracking" : "Pause Tracking";
    }

    private void ToggleStartup()
    {
        var desired = _startupItem.Checked;
        StartupManager.SetEnabled(desired);
        _startupItem.Checked = StartupManager.IsEnabled();
    }

    private void ToggleSound()
    {
        _notificationService.SoundEnabled = _soundItem.Checked;
    }

    private void OnBreakReminderRequested()
    {
        _notificationService.QueueBreakReminder();
    }

    private void OnReminderActionInvoked(ReminderAction action)
    {
        switch (action)
        {
            case ReminderAction.TakeBreak:
                _focusEngine.CompleteBreakFromAction();
                break;
            case ReminderAction.Snooze:
                _focusEngine.SnoozeBreakReminder();
                break;
            case ReminderAction.Skip:
                _focusEngine.SkipBreakReminder();
                break;
        }
    }

    private void OnForegroundWindowChanged(DateTime timestampUtc, IntPtr hwnd)
    {
        if (hwnd == _lastWindow || hwnd == IntPtr.Zero)
        {
            return;
        }

        _lastWindow = hwnd;
        _focusEngine.RegisterWindowSwitch(timestampUtc);
    }

    private void OnSnapshotChanged()
    {
        if (_dashboard is null || _dashboard.IsDisposed)
        {
            HandleBreakCountIncrease(_focusEngine.GetSnapshot());
            return;
        }

        var snapshot = _focusEngine.GetSnapshot();
        HandleBreakCountIncrease(snapshot);
        if (_dashboard.InvokeRequired)
        {
            _dashboard.BeginInvoke(new Action(() => _dashboard.Render(snapshot)));
            return;
        }

        _dashboard.Render(snapshot);
    }

    private void HandleBreakCountIncrease(FocusSnapshot snapshot)
    {
        if (_lastBreaksTakenObserved < 0)
        {
            _lastBreaksTakenObserved = snapshot.BreaksTaken;
            return;
        }

        if (snapshot.BreaksTaken > _lastBreaksTakenObserved)
        {
            _trayIcon.BalloonTipTitle = "Break Completed";
            _trayIcon.BalloonTipText = $"Nice. Break count increased to {snapshot.BreaksTaken}/{snapshot.BreaksDue}.";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.ShowBalloonTip(5000);
        }

        _lastBreaksTakenObserved = snapshot.BreaksTaken;
    }

    private void OpenDashboard()
    {
        if (_dashboard is null || _dashboard.IsDisposed)
        {
            var weeklyData = _statsStore.LoadRecentDays(7)
                .Select(x => new WeeklyTrendPoint(
                    x.DateLocal.Date,
                    x.FocusScore,
                    TimeSpan.FromSeconds(x.FocusSeconds).TotalHours,
                    x.BreaksTaken,
                    x.BreaksDue))
                .ToList();

            _dashboard = new DashboardForm(
                _settings.DarkModeEnabled,
                weeklyData,
                darkMode =>
                {
                    _settings.DarkModeEnabled = darkMode;
                    _settingsStore.Save(_settings);
                },
                ExportWeeklyCsv);
            _dashboard.FormClosed += (_, _) => _dashboard = null;
        }

        _dashboard.Render(_focusEngine.GetSnapshot());
        _dashboard.Show();
        _dashboard.WindowState = FormWindowState.Normal;
        _dashboard.BringToFront();
        _dashboard.Activate();
    }

    private void PersistStats()
    {
        _statsStore.Save(_focusEngine.ExportForSave());
    }

    private void ExportWeeklyCsv()
    {
        try
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Weekly Focus Report",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"micro-break-weekly-{DateTime.Today:yyyy-MM-dd}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            var output = _statsStore.ExportRecentDaysCsv(7, dialog.FileName);
            _trayIcon.BalloonTipTitle = "CSV Exported";
            _trayIcon.BalloonTipText = $"Weekly report saved to:\n{output}";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.ShowBalloonTip(7000);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export weekly CSV.\n{ex.Message}",
                "Export Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    protected override void ExitThreadCore()
    {
        PersistStats();
        _settingsStore.Save(_settings);
        _saveTimer.Stop();
        _saveTimer.Dispose();
        _notificationService.Dispose();
        _activityTracker.Dispose();
        _focusEngine.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }
}
