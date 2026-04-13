namespace MicroBreakReminder.Models;

internal sealed record FocusSnapshot(
    DateTime DateLocal,
    int FocusScore,
    int EyeStrainRiskScore,
    int AdaptiveBreakMinutes,
    TimeSpan PostureDueIn,
    TimeSpan HydrationDueIn,
    TimeSpan ActiveTime,
    TimeSpan FocusTime,
    TimeSpan IdleTime,
    int Interruptions,
    int IdleBursts,
    int AppSwitches,
    int BreaksDue,
    int BreaksTaken,
    int BreaksSnoozed,
    bool IsPaused,
    bool IsCurrentlyIdle,
    IReadOnlyList<double> HourlyActiveSeconds,
    IReadOnlyList<double> HourlyFocusSeconds,
    IReadOnlyList<int> MinuteStates
);
