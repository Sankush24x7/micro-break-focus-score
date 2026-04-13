using MicroBreakReminder.Models;

namespace MicroBreakReminder.Services;

internal sealed class FocusEngine : IDisposable
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan BreakComplianceIdle = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RapidSwitchWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SnoozeInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PostureCadence = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan HydrationCadence = TimeSpan.FromMinutes(90);

    private readonly object _gate = new();
    private readonly Queue<DateTime> _recentSwitchEvents = new();
    private readonly System.Threading.Timer _tickTimer;
    private readonly DailyStats _stats;

    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private DateTime _snoozeUntilUtc = DateTime.MinValue;
    private DateTime _lastBreakTakenUtc = DateTime.MinValue;
    private bool _isPaused;
    private bool _isCurrentlyIdle;
    private bool _breakReminderOutstanding;
    private bool _wasDistracted;
    private double _continuousActiveSeconds;
    private double _activeSincePostureSeconds;
    private double _activeSinceHydrationSeconds;
    private int _currentAdaptiveBreakMinutes = 20;
    private int _currentEyeStrainRisk = 10;

    public event Action? SnapshotChanged;
    public event Action? BreakReminderRequested;
    public event Action? PostureReminderRequested;
    public event Action? HydrationNudgeRequested;

    public FocusEngine(DailyStats initialStats)
    {
        _stats = initialStats;
        NormalizeStats();
        _tickTimer = new System.Threading.Timer(Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void RegisterActivity(DateTime timestampUtc)
    {
        lock (_gate)
        {
            _lastActivityUtc = timestampUtc;
        }
    }

    public void RegisterWindowSwitch(DateTime timestampUtc)
    {
        lock (_gate)
        {
            if (_isPaused)
            {
                return;
            }

            _stats.AppSwitches++;
            _recentSwitchEvents.Enqueue(timestampUtc);
        }

        SnapshotChanged?.Invoke();
    }

    public void CompleteBreakFromAction()
    {
        lock (_gate)
        {
            if (!_breakReminderOutstanding)
            {
                return;
            }

            CompleteBreakInternal(DateTime.UtcNow);
        }

        SnapshotChanged?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        lock (_gate)
        {
            _isPaused = paused;
            _lastTickUtc = DateTime.UtcNow;
            _lastActivityUtc = DateTime.UtcNow;
            _continuousActiveSeconds = 0;
            _isCurrentlyIdle = false;
            _breakReminderOutstanding = false;
        }

        SnapshotChanged?.Invoke();
    }

    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _isPaused;
            }
        }
    }

    public void SnoozeBreakReminder()
    {
        lock (_gate)
        {
            _stats.BreaksSnoozed++;
            _snoozeUntilUtc = DateTime.UtcNow.Add(SnoozeInterval);
            _breakReminderOutstanding = false;
        }

        SnapshotChanged?.Invoke();
    }

    public void SkipBreakReminder()
    {
        lock (_gate)
        {
            if (!_breakReminderOutstanding)
            {
                return;
            }

            _stats.BreaksSkipped++;
            _breakReminderOutstanding = false;
            _continuousActiveSeconds = 0;
            _snoozeUntilUtc = DateTime.UtcNow.Add(TimeSpan.FromMinutes(2));
        }

        SnapshotChanged?.Invoke();
    }

    public FocusSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            EnsureDateBoundary();
            return new FocusSnapshot(
                _stats.DateLocal,
                _stats.FocusScore,
                _currentEyeStrainRisk,
                _currentAdaptiveBreakMinutes,
                TimeSpan.FromSeconds(Math.Max(0, PostureCadence.TotalSeconds - _activeSincePostureSeconds)),
                TimeSpan.FromSeconds(Math.Max(0, HydrationCadence.TotalSeconds - _activeSinceHydrationSeconds)),
                TimeSpan.FromSeconds(_stats.ActiveSeconds),
                TimeSpan.FromSeconds(_stats.FocusSeconds),
                TimeSpan.FromSeconds(_stats.IdleSeconds),
                _stats.Interruptions,
                _stats.IdleBursts,
                _stats.AppSwitches,
                _stats.BreaksDue,
                _stats.BreaksTaken,
                _stats.BreaksSnoozed,
                _isPaused,
                _isCurrentlyIdle,
                _stats.HourlyActiveSeconds.ToArray(),
                _stats.HourlyFocusSeconds.ToArray(),
                _stats.MinuteStates.ToArray());
        }
    }

    public DailyStats ExportForSave()
    {
        lock (_gate)
        {
            EnsureDateBoundary();
            return new DailyStats
            {
                DateLocal = _stats.DateLocal,
                ActiveSeconds = _stats.ActiveSeconds,
                FocusSeconds = _stats.FocusSeconds,
                IdleSeconds = _stats.IdleSeconds,
                Interruptions = _stats.Interruptions,
                IdleBursts = _stats.IdleBursts,
                AppSwitches = _stats.AppSwitches,
                BreaksDue = _stats.BreaksDue,
                BreaksTaken = _stats.BreaksTaken,
                BreaksSnoozed = _stats.BreaksSnoozed,
                BreaksSkipped = _stats.BreaksSkipped,
                PostureRemindersShown = _stats.PostureRemindersShown,
                HydrationNudgesShown = _stats.HydrationNudgesShown,
                HourlyActiveSeconds = _stats.HourlyActiveSeconds.ToArray(),
                HourlyFocusSeconds = _stats.HourlyFocusSeconds.ToArray(),
                MinuteStates = _stats.MinuteStates.ToArray()
            };
        }
    }

    private void Tick(object? state)
    {
        var raiseSnapshot = false;
        var raiseBreakReminder = false;
        var raisePosture = false;
        var raiseHydration = false;

        lock (_gate)
        {
            EnsureDateBoundary();

            var nowUtc = DateTime.UtcNow;
            var delta = nowUtc - _lastTickUtc;
            if (delta <= TimeSpan.Zero)
            {
                return;
            }

            if (delta > TimeSpan.FromMinutes(2))
            {
                delta = TimeSpan.FromSeconds(1);
            }

            _lastTickUtc = nowUtc;
            if (_isPaused)
            {
                return;
            }

            while (_recentSwitchEvents.Count > 0 && nowUtc - _recentSwitchEvents.Peek() > RapidSwitchWindow)
            {
                _recentSwitchEvents.Dequeue();
            }

            var idleFor = nowUtc - _lastActivityUtc;
            var currentlyIdle = idleFor >= IdleThreshold;
            var isDistracted = !currentlyIdle && _recentSwitchEvents.Count >= 6;
            _currentAdaptiveBreakMinutes = ComputeAdaptiveBreakMinutes(nowUtc, isDistracted);
            _currentEyeStrainRisk = ComputeEyeStrainRisk(nowUtc, isDistracted);

            if (currentlyIdle)
            {
                _stats.IdleSeconds += delta.TotalSeconds;
                WriteMinuteState(SessionState.Idle);

                if (!_isCurrentlyIdle)
                {
                    _isCurrentlyIdle = true;
                    _stats.IdleBursts++;
                    _stats.Interruptions++;
                    _continuousActiveSeconds = 0;
                }

                if (_breakReminderOutstanding && idleFor >= BreakComplianceIdle)
                {
                    CompleteBreakInternal(nowUtc);
                }
            }
            else
            {
                if (_isCurrentlyIdle)
                {
                    _isCurrentlyIdle = false;
                }

                _stats.ActiveSeconds += delta.TotalSeconds;
                _stats.FocusSeconds += delta.TotalSeconds;
                _continuousActiveSeconds += delta.TotalSeconds;
                _activeSincePostureSeconds += delta.TotalSeconds;
                _activeSinceHydrationSeconds += delta.TotalSeconds;

                var localHour = DateTime.Now.Hour;
                _stats.HourlyActiveSeconds[localHour] += delta.TotalSeconds;
                _stats.HourlyFocusSeconds[localHour] += delta.TotalSeconds;

                if (isDistracted)
                {
                    WriteMinuteState(SessionState.Distracted);
                    if (!_wasDistracted)
                    {
                        _stats.Interruptions++;
                        _wasDistracted = true;
                    }
                }
                else
                {
                    WriteMinuteState(SessionState.Focus);
                    _wasDistracted = false;
                }

                if (_activeSincePostureSeconds >= PostureCadence.TotalSeconds)
                {
                    _stats.PostureRemindersShown++;
                    _activeSincePostureSeconds = 0;
                    raisePosture = true;
                }

                if (_activeSinceHydrationSeconds >= HydrationCadence.TotalSeconds)
                {
                    _stats.HydrationNudgesShown++;
                    _activeSinceHydrationSeconds = 0;
                    raiseHydration = true;
                }

                if (!_breakReminderOutstanding &&
                    _continuousActiveSeconds >= _currentAdaptiveBreakMinutes * 60 &&
                    nowUtc >= _snoozeUntilUtc)
                {
                    _stats.BreaksDue++;
                    _breakReminderOutstanding = true;
                    raiseBreakReminder = true;
                }
            }

            if (_lastBreakTakenUtc != DateTime.MinValue && nowUtc - _lastBreakTakenUtc <= TimeSpan.FromMinutes(2))
            {
                WriteMinuteState(SessionState.Break);
            }

            raiseSnapshot = true;
        }

        if (raiseSnapshot)
        {
            SnapshotChanged?.Invoke();
        }

        if (raiseBreakReminder)
        {
            BreakReminderRequested?.Invoke();
        }

        if (raisePosture)
        {
            PostureReminderRequested?.Invoke();
        }

        if (raiseHydration)
        {
            HydrationNudgeRequested?.Invoke();
        }
    }

    private void CompleteBreakInternal(DateTime nowUtc)
    {
        _stats.BreaksTaken++;
        _breakReminderOutstanding = false;
        _continuousActiveSeconds = 0;
        _lastBreakTakenUtc = nowUtc;
        _activeSincePostureSeconds = Math.Max(0, _activeSincePostureSeconds - 600);
        _activeSinceHydrationSeconds = Math.Max(0, _activeSinceHydrationSeconds - 600);
        WriteMinuteState(SessionState.Break);
    }

    private int ComputeAdaptiveBreakMinutes(DateTime nowUtc, bool isDistracted)
    {
        var minutes = 20;
        if (_continuousActiveSeconds >= TimeSpan.FromMinutes(40).TotalSeconds)
        {
            minutes -= 3;
        }

        if (isDistracted)
        {
            minutes -= 2;
        }

        var localHour = nowUtc.ToLocalTime().Hour;
        if (localHour >= 20 || localHour <= 6)
        {
            minutes -= 2;
        }

        if (_currentEyeStrainRisk >= 70)
        {
            minutes -= 2;
        }

        return Math.Clamp(minutes, 12, 25);
    }

    private int ComputeEyeStrainRisk(DateTime nowUtc, bool isDistracted)
    {
        var continuousMinutes = _continuousActiveSeconds / 60.0;
        var activeHours = _stats.ActiveSeconds / 3600.0;
        var breakCompliancePenalty = _stats.BreaksDue > 0 ? (1 - (double)_stats.BreaksTaken / _stats.BreaksDue) * 25 : 5;
        var risk = 10 + (continuousMinutes * 1.8) + (Math.Max(0, activeHours - 3) * 6) + breakCompliancePenalty;
        if (isDistracted)
        {
            risk += 8;
        }

        var localHour = nowUtc.ToLocalTime().Hour;
        if (localHour >= 21 || localHour <= 5)
        {
            risk += 10;
        }

        return Math.Clamp((int)Math.Round(risk), 0, 100);
    }

    private void WriteMinuteState(SessionState state)
    {
        var minuteIndex = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
        if (minuteIndex < 0 || minuteIndex >= _stats.MinuteStates.Length)
        {
            return;
        }

        // Keep the most "intense" state observed inside the minute.
        _stats.MinuteStates[minuteIndex] = Math.Max(_stats.MinuteStates[minuteIndex], (int)state);
    }

    private void EnsureDateBoundary()
    {
        if (_stats.DateLocal.Date == DateTime.Today)
        {
            return;
        }

        _stats.DateLocal = DateTime.Today;
        _stats.ActiveSeconds = 0;
        _stats.FocusSeconds = 0;
        _stats.IdleSeconds = 0;
        _stats.Interruptions = 0;
        _stats.IdleBursts = 0;
        _stats.AppSwitches = 0;
        _stats.BreaksDue = 0;
        _stats.BreaksTaken = 0;
        _stats.BreaksSnoozed = 0;
        _stats.BreaksSkipped = 0;
        _stats.PostureRemindersShown = 0;
        _stats.HydrationNudgesShown = 0;
        _stats.HourlyActiveSeconds = new double[24];
        _stats.HourlyFocusSeconds = new double[24];
        _stats.MinuteStates = new int[24 * 60];
        _continuousActiveSeconds = 0;
        _breakReminderOutstanding = false;
        _isCurrentlyIdle = false;
        _wasDistracted = false;
        _activeSincePostureSeconds = 0;
        _activeSinceHydrationSeconds = 0;
        _recentSwitchEvents.Clear();
    }

    private void NormalizeStats()
    {
        if (_stats.HourlyActiveSeconds.Length != 24)
        {
            _stats.HourlyActiveSeconds = new double[24];
        }

        if (_stats.HourlyFocusSeconds.Length != 24)
        {
            _stats.HourlyFocusSeconds = new double[24];
        }

        if (_stats.MinuteStates.Length != 24 * 60)
        {
            _stats.MinuteStates = new int[24 * 60];
        }
    }

    public void Dispose()
    {
        _tickTimer.Dispose();
    }
}
