namespace MicroBreakReminder.Models;

internal sealed record WeeklyTrendPoint(
    DateTime DateLocal,
    int FocusScore,
    double FocusHours,
    int BreaksTaken,
    int BreaksDue
);
