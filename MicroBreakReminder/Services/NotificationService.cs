using System.Media;
using System.Runtime.InteropServices;

namespace MicroBreakReminder.Services;

internal sealed class NotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Queue<DateTime> _queuedReminders = new();
    private readonly System.Windows.Forms.Timer _queueTimer;
    private bool _soundEnabled = true;
    private BreakActionToastForm? _activeToast;

    public event Action<ReminderAction>? ReminderActionInvoked;

    public NotificationService(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
        _queueTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _queueTimer.Tick += (_, _) => TryFlushQueue();
        _queueTimer.Start();
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => _soundEnabled = value;
    }

    public void QueueBreakReminder()
    {
        _queuedReminders.Enqueue(DateTime.Now);
        TryFlushQueue();
    }

    public void ShowPostureReminder()
    {
        ShowBalloon("Posture Check", "Roll your shoulders back and sit upright for 20 seconds.");
    }

    public void ShowHydrationNudge()
    {
        ShowBalloon("Hydration Nudge", "Take a few sips of water to stay sharp.");
    }

    private void TryFlushQueue()
    {
        if (_queuedReminders.Count == 0)
        {
            return;
        }

        if (IsForegroundFullscreen())
        {
            return;
        }

        _ = _queuedReminders.Dequeue();
        ShowActionToast();
        if (_soundEnabled)
        {
            SystemSounds.Asterisk.Play();
        }
    }

    private void ShowActionToast()
    {
        if (_activeToast is not null && !_activeToast.IsDisposed)
        {
            _activeToast.Close();
        }

        _activeToast = new BreakActionToastForm();
        _activeToast.ActionSelected += action => ReminderActionInvoked?.Invoke(action);
        _activeToast.FormClosed += (_, _) => _activeToast = null;
        _activeToast.Show();

        ShowBalloon("Micro-Break Reminder", "Look at something 20 feet away for 20 seconds.");
    }

    private void ShowBalloon(string title, string text)
    {
        if (IsForegroundFullscreen())
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(7000);
    }

    private static bool IsForegroundFullscreen()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (!GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(hwnd);
        var bounds = screen.Bounds;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        return Math.Abs(width - bounds.Width) <= 2 && Math.Abs(height - bounds.Height) <= 2;
    }

    public void Dispose()
    {
        _queueTimer.Stop();
        _queueTimer.Dispose();
        if (_activeToast is not null && !_activeToast.IsDisposed)
        {
            _activeToast.Close();
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
