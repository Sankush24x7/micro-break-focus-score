using MicroBreakReminder.Models;

namespace MicroBreakReminder.UI;

internal sealed class SessionTimelineControl : Control
{
    private IReadOnlyList<int> _minuteStates = Array.Empty<int>();
    private UiTheme _theme = UiTheme.Light;

    public SessionTimelineControl()
    {
        DoubleBuffered = true;
    }

    public void SetTheme(UiTheme theme)
    {
        _theme = theme;
        BackColor = theme.Surface;
        Invalidate();
    }

    public void SetMinuteStates(IReadOnlyList<int> minuteStates)
    {
        _minuteStates = minuteStates;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(_theme.Surface);

        using var borderPen = new Pen(_theme.Border);
        using var textBrush = new SolidBrush(_theme.TextSecondary);
        var rect = new Rectangle(8, 6, Width - 16, Height - 24);
        g.DrawRectangle(borderPen, rect);

        if (_minuteStates.Count == 0)
        {
            g.DrawString("Session timeline will appear as you work.", Font, textBrush, new PointF(12, 10));
            return;
        }

        var minuteCount = Math.Min(_minuteStates.Count, 24 * 60);
        var pixels = Math.Max(1, rect.Width);
        for (var px = 0; px < pixels; px++)
        {
            var startMinute = px * minuteCount / pixels;
            var endMinute = Math.Max(startMinute + 1, (px + 1) * minuteCount / pixels);
            var state = 0;
            for (var i = startMinute; i < endMinute && i < minuteCount; i++)
            {
                state = Math.Max(state, _minuteStates[i]);
            }

            using var brush = new SolidBrush(MapColor((SessionState)state));
            g.FillRectangle(brush, rect.Left + px, rect.Top + 1, 1, rect.Height - 1);
        }

        g.DrawString("0h", Font, textBrush, new PointF(rect.Left, rect.Bottom + 2));
        g.DrawString("6h", Font, textBrush, new PointF(rect.Left + rect.Width * 0.25f - 8, rect.Bottom + 2));
        g.DrawString("12h", Font, textBrush, new PointF(rect.Left + rect.Width * 0.5f - 10, rect.Bottom + 2));
        g.DrawString("18h", Font, textBrush, new PointF(rect.Left + rect.Width * 0.75f - 10, rect.Bottom + 2));
        g.DrawString("23h", Font, textBrush, new PointF(rect.Right - 22, rect.Bottom + 2));
    }

    private Color MapColor(SessionState state)
    {
        return state switch
        {
            SessionState.Focus => Color.FromArgb(130, _theme.AccentGreen),
            SessionState.Distracted => Color.FromArgb(160, 245, 167, 42),
            SessionState.Idle => Color.FromArgb(170, 153, 163, 181),
            SessionState.Break => Color.FromArgb(180, _theme.AccentBlue),
            _ => Color.FromArgb(60, _theme.Border)
        };
    }
}
