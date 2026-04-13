using System.Drawing.Drawing2D;
using MicroBreakReminder.Models;

namespace MicroBreakReminder.UI;

internal sealed class WeeklyTrendControl : Control
{
    private IReadOnlyList<WeeklyTrendPoint> _points = Array.Empty<WeeklyTrendPoint>();
    private UiTheme _theme = UiTheme.Light;
    private readonly ToolTip _tooltip = new();
    private readonly List<(RectangleF Rect, WeeklyTrendPoint Point)> _hitAreas = new();
    private int _lastTooltipIndex = -1;

    public WeeklyTrendControl()
    {
        DoubleBuffered = true;
    }

    public void SetData(IReadOnlyList<WeeklyTrendPoint> points)
    {
        _points = points;
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
        using var barBrush = new SolidBrush(Color.FromArgb(120, _theme.AccentBlue));
        using var linePen = new Pen(_theme.AccentGreen, 2f);
        using var pointBrush = new SolidBrush(_theme.AccentGreen);
        using var textBrush = new SolidBrush(_theme.TextSecondary);

        var plotRect = new Rectangle(10, 8, Width - 20, Height - 28);
        g.DrawRectangle(borderPen, plotRect);

        for (var i = 1; i < 4; i++)
        {
            var y = plotRect.Top + i * plotRect.Height / 4;
            g.DrawLine(gridPen, plotRect.Left + 1, y, plotRect.Right - 1, y);
        }

        if (_points.Count == 0)
        {
            g.DrawString("No weekly data yet", Font, textBrush, new PointF(14, 12));
            return;
        }

        _hitAreas.Clear();
        var barWidth = Math.Max(14f, (plotRect.Width / 7f) - 10f);
        var pointPositions = new List<PointF>(_points.Count);
        for (var i = 0; i < _points.Count; i++)
        {
            var x = plotRect.Left + i * (plotRect.Width / 7f) + ((plotRect.Width / 7f) - barWidth) / 2f;
            var barHeight = (float)(_points[i].FocusScore / 100d * (plotRect.Height - 4));
            var rect = new RectangleF(x, plotRect.Bottom - barHeight, barWidth, barHeight);
            g.FillRectangle(barBrush, rect);
            _hitAreas.Add((rect, _points[i]));

            var lineX = x + barWidth / 2f;
            var lineY = plotRect.Bottom - barHeight;
            pointPositions.Add(new PointF(lineX, lineY));

            var dayText = _points[i].DateLocal.ToString("ddd");
            g.DrawString(dayText, Font, textBrush, new PointF(x - 2, plotRect.Bottom + 2));
        }

        if (pointPositions.Count >= 2)
        {
            g.DrawLines(linePen, pointPositions.ToArray());
        }

        foreach (var pt in pointPositions)
        {
            g.FillEllipse(pointBrush, pt.X - 3, pt.Y - 3, 6, 6);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        for (var i = 0; i < _hitAreas.Count; i++)
        {
            if (_hitAreas[i].Rect.Contains(e.Location))
            {
                if (_lastTooltipIndex == i)
                {
                    return;
                }

                _lastTooltipIndex = i;
                var point = _hitAreas[i].Point;
                var text =
                    $"{point.DateLocal:ddd, dd MMM yyyy}\n" +
                    $"Focus Score: {point.FocusScore}/100\n" +
                    $"Focus Hours: {point.FocusHours:F2}h\n" +
                    $"Breaks: {point.BreaksTaken}/{point.BreaksDue}";
                _tooltip.Show(text, this, e.Location.X + 12, e.Location.Y + 12, 3000);
                return;
            }
        }

        _lastTooltipIndex = -1;
        _tooltip.Hide(this);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _lastTooltipIndex = -1;
        _tooltip.Hide(this);
    }
}
