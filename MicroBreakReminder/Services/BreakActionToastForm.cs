namespace MicroBreakReminder.Services;

internal sealed class BreakActionToastForm : Form
{
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    public event Action<ReminderAction>? ActionSelected;

    public BreakActionToastForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Width = 360;
        Height = 140;
        BackColor = Color.FromArgb(26, 33, 45);

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(screen.Right - Width - 18, screen.Bottom - Height - 18);

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Text = "Micro-Break Reminder",
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold)
        };
        Controls.Add(title);

        var body = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Look at something 20 feet away for 20 seconds.",
            ForeColor = Color.FromArgb(224, 231, 245),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 10, 0)
        };
        Controls.Add(body);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(10, 0, 10, 10)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        Controls.Add(buttons);

        buttons.Controls.Add(BuildButton("Take Break", ReminderAction.TakeBreak), 0, 0);
        buttons.Controls.Add(BuildButton("Snooze 5m", ReminderAction.Snooze), 1, 0);
        buttons.Controls.Add(BuildButton("Skip", ReminderAction.Skip), 2, 0);

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
        _autoCloseTimer.Tick += (_, _) => Close();
        _autoCloseTimer.Start();
    }

    private Button BuildButton(string text, ReminderAction action)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 52, 70),
            ForeColor = Color.White,
            Margin = new Padding(6, 2, 6, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(66, 84, 108);
        button.Click += (_, _) =>
        {
            ActionSelected?.Invoke(action);
            Close();
        };
        return button;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
