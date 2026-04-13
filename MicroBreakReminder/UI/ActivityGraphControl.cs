using System.Drawing.Drawing2D;
using MicroBreakReminder.Models;

namespace MicroBreakReminder.UI;

internal sealed class ActivityGraphControl : Control
{
    private FocusSnapshot? _snapshot;
    private UiTheme _theme = UiTheme.Light;

    public ActivityGraphControl()
    {
        DoubleBuffered = true;
    }

    public void SetSnapshot(FocusSnapshot snapshot)
    {
        _snapshot = snapshot;
        Invalidate();
    }

    public void SetTheme(UiTheme theme)
    {
        _theme = theme;
        BackColor = theme.Surface;
        ForeColor = theme.TextPrimary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.Clear(_theme.Surface);

        using var borderPen = new Pen(_theme.Border);
        using var gridPen = new Pen(_theme.GraphGrid);
        using var axisTextBrush = new SolidBrush(_theme.TextSecondary);
        using var activeBrush = new SolidBrush(_theme.AccentBlue);
        using var focusBrush = new SolidBrush(_theme.AccentGreen);

        var plotRect = new Rectangle(12, 8, Width - 24, Height - 36);
        g.DrawRectangle(borderPen, plotRect);

        for (var i = 1; i < 4; i++)
        {
            var y = plotRect.Top + i * plotRect.Height / 4;
            g.DrawLine(gridPen, plotRect.Left + 1, y, plotRect.Right - 1, y);
        }

        if (_snapshot is null || _snapshot.HourlyActiveSeconds.Sum() <= 0)
        {
            g.DrawString("No activity yet. Start working and data will appear here.", Font, axisTextBrush, new PointF(16, 14));
            return;
        }

        var scaleMaxSeconds = 3600.0;
        var barWidth = Math.Max(3f, (float)plotRect.Width / 24f - 4f);
        for (var hour = 0; hour < 24; hour++)
        {
            var x = plotRect.Left + hour * (barWidth + 4f) + 1f;
            var activeHeight = (float)(Math.Min(_snapshot.HourlyActiveSeconds[hour], scaleMaxSeconds) / scaleMaxSeconds * (plotRect.Height - 2));
            var focusHeight = (float)(Math.Min(_snapshot.HourlyFocusSeconds[hour], scaleMaxSeconds) / scaleMaxSeconds * (plotRect.Height - 2));
            if (_snapshot.HourlyActiveSeconds[hour] > 0 && activeHeight < 2f)
            {
                activeHeight = 2f;
            }

            if (_snapshot.HourlyFocusSeconds[hour] > 0 && focusHeight < 2f)
            {
                focusHeight = 2f;
            }

            var activeRect = new RectangleF(x, plotRect.Bottom - activeHeight, barWidth, activeHeight);
            var focusRect = new RectangleF(x, plotRect.Bottom - focusHeight, barWidth, focusHeight);

            g.FillRectangle(activeBrush, activeRect);
            g.FillRectangle(focusBrush, focusRect);
        }

        DrawAxisLabel(g, axisTextBrush, "0h", plotRect.Left, plotRect.Bottom + 3);
        DrawAxisLabel(g, axisTextBrush, "6h", plotRect.Left + plotRect.Width * 0.25f - 8, plotRect.Bottom + 3);
        DrawAxisLabel(g, axisTextBrush, "12h", plotRect.Left + plotRect.Width * 0.5f - 12, plotRect.Bottom + 3);
        DrawAxisLabel(g, axisTextBrush, "18h", plotRect.Left + plotRect.Width * 0.75f - 12, plotRect.Bottom + 3);
        DrawAxisLabel(g, axisTextBrush, "23h", plotRect.Right - 26, plotRect.Bottom + 3);
    }

    private void DrawAxisLabel(Graphics g, Brush brush, string text, float x, float y)
    {
        g.DrawString(text, Font, brush, new PointF(x, y));
    }
}
