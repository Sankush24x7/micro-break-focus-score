namespace MicroBreakReminder.Models;

internal sealed class DailyStats
{
    public DateTime DateLocal { get; set; } = DateTime.Today;
    public double ActiveSeconds { get; set; }
    public double FocusSeconds { get; set; }
    public double IdleSeconds { get; set; }
    public int Interruptions { get; set; }
    public int IdleBursts { get; set; }
    public int AppSwitches { get; set; }
    public int BreaksDue { get; set; }
    public int BreaksTaken { get; set; }
    public int BreaksSnoozed { get; set; }
    public int BreaksSkipped { get; set; }
    public int PostureRemindersShown { get; set; }
    public int HydrationNudgesShown { get; set; }
    public double[] HourlyActiveSeconds { get; set; } = new double[24];
    public double[] HourlyFocusSeconds { get; set; } = new double[24];
    public int[] MinuteStates { get; set; } = new int[24 * 60];

    public int FocusScore
    {
        get
        {
            var focusRatio = ActiveSeconds > 0 ? FocusSeconds / ActiveSeconds : 0;
            var breakRatio = BreaksDue > 0 ? (double)BreaksTaken / BreaksDue : 1;
            var interruptionPenalty = Math.Min(35, Interruptions * 1.5);
            var score = (focusRatio * 60) + (breakRatio * 30) + 10 - interruptionPenalty;
            return Math.Clamp((int)Math.Round(score), 0, 100);
        }
    }
}
