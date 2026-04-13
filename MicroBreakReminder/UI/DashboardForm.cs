using MicroBreakReminder.Models;

namespace MicroBreakReminder.UI;

internal sealed class DashboardForm : Form
{
    private readonly Label _scoreLabel;
    private readonly Label _focusTimeLabel;
    private readonly Label _activeTimeLabel;
    private readonly Label _breakLabel;
    private readonly Label _statusLabel;
    private readonly Label _bannerLabel;
    private readonly Button _infoButton;
    private readonly CheckBox _themeToggle;
    private readonly ActivityGraphControl _graph;
    private readonly SessionTimelineControl _timeline;
    private readonly WeeklyTrendControl _weeklyTrend;
    private readonly Label _healthMetricsLabel;
    private readonly Label _timelineCaption;
    private readonly Label _graphCaption;
    private readonly Label _weeklyCaption;
    private readonly Button _exportCsvButton;

    private readonly List<Panel> _cards = new();
    private readonly List<Label> _cardTitles = new();
    private readonly List<Label> _cardValues = new();
    private readonly TableLayoutPanel _root;
    private readonly Panel _bannerPanel;
    private readonly Panel _graphPanel;
    private readonly Panel _timelinePanel;
    private readonly Panel _weeklyPanel;
    private readonly Action<bool> _onThemeChanged;
    private readonly Action _onExportWeeklyCsv;

    private readonly List<WeeklyTrendPoint> _weeklyPoints;

    private UiTheme _theme = UiTheme.Light;
    private UiTheme _targetTheme = UiTheme.Light;
    private readonly System.Windows.Forms.Timer _themeAnimationTimer;
    private int _themeAnimationStep;
    private const int ThemeAnimationFrames = 10;
    private bool _suppressThemeChanged;

    public DashboardForm(
        bool darkModeEnabled,
        IReadOnlyList<WeeklyTrendPoint> weeklyTrendData,
        Action<bool> onThemeChanged,
        Action onExportWeeklyCsv)
    {
        _onThemeChanged = onThemeChanged;
        _onExportWeeklyCsv = onExportWeeklyCsv;
        _weeklyPoints = weeklyTrendData.ToList();

        Text = "Micro-Break Reminder Dashboard";
        Width = 980;
        Height = 760;
        MinimumSize = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);

        _themeAnimationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _themeAnimationTimer.Tick += (_, _) => OnThemeAnimationTick();

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        Controls.Add(_root);

        _bannerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10) };
        _root.Controls.Add(_bannerPanel, 0, 0);

        var bannerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3
        };
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        _bannerPanel.Controls.Add(bannerLayout);

        _bannerLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "This app monitors keyboard and mouse activity locally to improve productivity. No personal data is collected or transmitted."
        };
        bannerLayout.Controls.Add(_bannerLabel, 0, 0);

        _themeToggle = new CheckBox
        {
            Appearance = Appearance.Button,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Dark",
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8)
        };
        _themeToggle.FlatAppearance.BorderSize = 1;
        _themeToggle.CheckedChanged += (_, _) =>
        {
            if (_suppressThemeChanged)
            {
                return;
            }

            StartThemeTransition(_themeToggle.Checked ? UiTheme.Dark : UiTheme.Light);
            _onThemeChanged(_themeToggle.Checked);
        };
        bannerLayout.Controls.Add(_themeToggle, 1, 0);

        _infoButton = new Button
        {
            Text = "i",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            Margin = new Padding(6, 8, 0, 8)
        };
        _infoButton.FlatAppearance.BorderSize = 1;
        _infoButton.Click += (_, _) => ShowInfoModal();
        bannerLayout.Controls.Add(_infoButton, 2, 0);

        var metricsPanel = BuildMetricsPanel();
        _root.Controls.Add(metricsPanel, 0, 1);

        _graphPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8), BorderStyle = BorderStyle.FixedSingle };
        _root.Controls.Add(_graphPanel, 0, 2);
        _graph = new ActivityGraphControl { Dock = DockStyle.Fill };
        _graphPanel.Controls.Add(_graph);
        _graphCaption = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Text = "Hourly activity (blue) vs focus (green). Scale: 0 to 60 minutes per hour."
        };
        _graphPanel.Controls.Add(_graphCaption);

        _timelinePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 6), BorderStyle = BorderStyle.FixedSingle };
        _root.Controls.Add(_timelinePanel, 0, 3);
        _timeline = new SessionTimelineControl { Dock = DockStyle.Fill };
        _timelinePanel.Controls.Add(_timeline);
        _timelineCaption = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Text = "Daily timeline: focus (green), distracted (amber), idle (gray), breaks (blue)."
        };
        _timelinePanel.Controls.Add(_timelineCaption);
        _healthMetricsLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(0, 2, 0, 0),
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Eye strain risk: 0/100 | Adaptive break: 20 min | Posture in 45m | Hydration in 90m"
        };
        _timelinePanel.Controls.Add(_healthMetricsLabel);
        _timeline.BringToFront();

        _weeklyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8), BorderStyle = BorderStyle.FixedSingle };
        _root.Controls.Add(_weeklyPanel, 0, 4);

        var weeklyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        weeklyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        weeklyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _weeklyPanel.Controls.Add(weeklyLayout);

        var weeklyHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        weeklyHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        weeklyHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
        weeklyLayout.Controls.Add(weeklyHeader, 0, 0);

        _weeklyCaption = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            Text = "Weekly trend (last 7 days): bars = focus score, line = trend direction."
        };
        weeklyHeader.Controls.Add(_weeklyCaption, 0, 0);

        _exportCsvButton = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Export CSV",
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 2),
            MinimumSize = new Size(110, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _exportCsvButton.FlatAppearance.BorderSize = 1;
        _exportCsvButton.Click += (_, _) => _onExportWeeklyCsv();
        weeklyHeader.Controls.Add(_exportCsvButton, 1, 0);

        _weeklyTrend = new WeeklyTrendControl { Dock = DockStyle.Fill };
        weeklyLayout.Controls.Add(_weeklyTrend, 0, 1);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Regular)
        };
        _root.Controls.Add(_statusLabel, 0, 5);

        _scoreLabel = FindRequiredLabel(metricsPanel, "ScoreValue");
        _focusTimeLabel = FindRequiredLabel(metricsPanel, "FocusValue");
        _activeTimeLabel = FindRequiredLabel(metricsPanel, "ActiveValue");
        _breakLabel = FindRequiredLabel(metricsPanel, "BreakValue");

        ApplyThemeInstant(UiTheme.Light);
        _suppressThemeChanged = true;
        _themeToggle.Checked = darkModeEnabled;
        _suppressThemeChanged = false;
        ApplyThemeInstant(darkModeEnabled ? UiTheme.Dark : UiTheme.Light);
        _weeklyTrend.SetData(_weeklyPoints);
    }

    public void Render(FocusSnapshot snapshot)
    {
        _scoreLabel.Text = $"{snapshot.FocusScore}/100";
        _focusTimeLabel.Text = FormatDuration(snapshot.FocusTime);
        _activeTimeLabel.Text = FormatDuration(snapshot.ActiveTime);
        _breakLabel.Text = $"{snapshot.BreaksTaken}/{snapshot.BreaksDue}";

        if (snapshot.ActiveTime.TotalMinutes < 10)
        {
            _statusLabel.Text = "Tracking is active. Data is still being collected, so the chart starts small for the first few minutes.";
        }
        else
        {
            _statusLabel.Text = snapshot.IsPaused
                ? "Tracking is paused."
                : snapshot.IsCurrentlyIdle
                    ? "User is currently idle."
                    : "Tracking is active.";
        }

        _graph.SetSnapshot(snapshot);
        _timeline.SetMinuteStates(snapshot.MinuteStates);
        _healthMetricsLabel.Text =
            $"Eye strain risk: {snapshot.EyeStrainRiskScore}/100 | Adaptive break: {snapshot.AdaptiveBreakMinutes} min | " +
            $"Posture in {FormatCompact(snapshot.PostureDueIn)} | Hydration in {FormatCompact(snapshot.HydrationDueIn)}";
        UpdateWeeklyTodayPoint(snapshot);
        _weeklyTrend.SetData(_weeklyPoints);
    }

    private void UpdateWeeklyTodayPoint(FocusSnapshot snapshot)
    {
        var today = DateTime.Today;
        var index = _weeklyPoints.FindIndex(x => x.DateLocal.Date == today);
        var newPoint = new WeeklyTrendPoint(
            today,
            snapshot.FocusScore,
            snapshot.FocusTime.TotalHours,
            snapshot.BreaksTaken,
            snapshot.BreaksDue);

        if (index >= 0)
        {
            _weeklyPoints[index] = newPoint;
        }
        else
        {
            _weeklyPoints.Add(newPoint);
            if (_weeklyPoints.Count > 7)
            {
                _weeklyPoints.RemoveAt(0);
            }
        }
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
        }

        return $"{span.Minutes}m {span.Seconds:D2}s";
    }

    private static string FormatCompact(TimeSpan span)
    {
        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
        }

        return $"{Math.Max(0, span.Minutes)}m";
    }

    private TableLayoutPanel BuildMetricsPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 10),
            Padding = new Padding(2, 0, 2, 0),
            ColumnCount = 4,
            RowCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        layout.Controls.Add(BuildMetricCard("Focus Score", "0/100", "ScoreValue"), 0, 0);
        layout.Controls.Add(BuildMetricCard("Focus Time", "0m", "FocusValue"), 1, 0);
        layout.Controls.Add(BuildMetricCard("Active Time", "0m", "ActiveValue"), 2, 0);
        layout.Controls.Add(BuildMetricCard("Breaks Taken", "0/0", "BreakValue"), 3, 0);
        return layout;
    }

    private Panel BuildMetricCard(string title, string value, string valueName)
    {
        var card = new Panel
        {
            Margin = new Padding(8),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BorderStyle = BorderStyle.FixedSingle
        };
        _cards.Add(card);

        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(inner);

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11f, FontStyle.Regular)
        };
        _cardTitles.Add(titleLabel);
        inner.Controls.Add(titleLabel, 0, 0);

        var valueLabel = new Label
        {
            Name = valueName,
            Dock = DockStyle.Fill,
            Text = value,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI Semibold", 19f, FontStyle.Bold)
        };
        _cardValues.Add(valueLabel);
        inner.Controls.Add(valueLabel, 0, 1);

        return card;
    }

    private void StartThemeTransition(UiTheme nextTheme)
    {
        _targetTheme = nextTheme;
        _themeAnimationStep = 0;
        _themeAnimationTimer.Start();
    }

    private void OnThemeAnimationTick()
    {
        _themeAnimationStep++;
        var t = Math.Min(1f, _themeAnimationStep / (float)ThemeAnimationFrames);
        var blended = UiTheme.Blend(_theme, _targetTheme, t);
        ApplyThemeInstant(blended);

        if (t >= 1f)
        {
            _themeAnimationTimer.Stop();
            _theme = _targetTheme;
            ApplyThemeInstant(_theme);
        }
    }

    private void ApplyThemeInstant(UiTheme theme)
    {
        BackColor = theme.WindowBackground;
        ForeColor = theme.TextPrimary;
        _root.BackColor = theme.WindowBackground;

        _bannerPanel.BackColor = theme.BannerBackground;
        _bannerLabel.ForeColor = theme.BannerText;
        _themeToggle.BackColor = theme.Surface;
        _themeToggle.ForeColor = theme.TextPrimary;
        _themeToggle.FlatAppearance.BorderColor = theme.Border;
        _infoButton.BackColor = theme.Surface;
        _infoButton.ForeColor = theme.TextPrimary;
        _infoButton.FlatAppearance.BorderColor = theme.Border;
        _exportCsvButton.BackColor = theme.Surface;
        _exportCsvButton.ForeColor = theme.TextPrimary;
        _exportCsvButton.FlatAppearance.BorderColor = theme.Border;

        foreach (var card in _cards)
        {
            card.BackColor = theme.Surface;
        }

        foreach (var label in _cardTitles)
        {
            label.ForeColor = theme.TextSecondary;
            label.BackColor = theme.Surface;
        }

        foreach (var label in _cardValues)
        {
            label.ForeColor = theme.TextPrimary;
            label.BackColor = theme.Surface;
        }

        _graphPanel.BackColor = theme.SurfaceAlt;
        _timelinePanel.BackColor = theme.SurfaceAlt;
        _weeklyPanel.BackColor = theme.SurfaceAlt;
        _graphCaption.BackColor = theme.SurfaceAlt;
        _graphCaption.ForeColor = theme.TextSecondary;
        _timelineCaption.BackColor = theme.SurfaceAlt;
        _timelineCaption.ForeColor = theme.TextPrimary;
        _healthMetricsLabel.BackColor = theme.SurfaceAlt;
        _healthMetricsLabel.ForeColor = theme.TextPrimary;
        _weeklyCaption.BackColor = theme.SurfaceAlt;
        _weeklyCaption.ForeColor = theme.TextPrimary;
        _statusLabel.ForeColor = theme.TextPrimary;
        _statusLabel.BackColor = theme.WindowBackground;

        _graph.SetTheme(theme);
        _timeline.SetTheme(theme);
        _weeklyTrend.SetTheme(theme);
    }

    private static Label FindRequiredLabel(Control root, string name)
    {
        var control = FindControlRecursive(root, name);
        if (control is Label label)
        {
            return label;
        }

        throw new InvalidOperationException($"Could not find required label '{name}'.");
    }

    private static Control? FindControlRecursive(Control parent, string name)
    {
        foreach (Control control in parent.Controls)
        {
            if (string.Equals(control.Name, name, StringComparison.Ordinal))
            {
                return control;
            }

            var nested = FindControlRecursive(control, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private void ShowInfoModal()
    {
        MessageBox.Show(
            "Purpose:\nThis app helps reduce eye strain and improve productivity by nudging short breaks.\n\n" +
            "Focus Score:\nScore is based on focus-time ratio, break adherence, and interruptions.\n\n" +
            "Privacy:\nNo keystrokes are stored. No typed content is collected. Data is processed locally only.",
            "About Micro-Break Reminder",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
