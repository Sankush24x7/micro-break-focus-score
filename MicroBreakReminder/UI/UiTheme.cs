namespace MicroBreakReminder.UI;

internal sealed class UiTheme
{
    public UiTheme(
        Color windowBackground,
        Color surface,
        Color surfaceAlt,
        Color textPrimary,
        Color textSecondary,
        Color border,
        Color bannerBackground,
        Color bannerText,
        Color accentBlue,
        Color accentGreen,
        Color graphGrid)
    {
        WindowBackground = windowBackground;
        Surface = surface;
        SurfaceAlt = surfaceAlt;
        TextPrimary = textPrimary;
        TextSecondary = textSecondary;
        Border = border;
        BannerBackground = bannerBackground;
        BannerText = bannerText;
        AccentBlue = accentBlue;
        AccentGreen = accentGreen;
        GraphGrid = graphGrid;
    }

    public Color WindowBackground { get; }
    public Color Surface { get; }
    public Color SurfaceAlt { get; }
    public Color TextPrimary { get; }
    public Color TextSecondary { get; }
    public Color Border { get; }
    public Color BannerBackground { get; }
    public Color BannerText { get; }
    public Color AccentBlue { get; }
    public Color AccentGreen { get; }
    public Color GraphGrid { get; }

    public static UiTheme Light { get; } = new(
        Color.FromArgb(237, 242, 250),
        Color.White,
        Color.FromArgb(244, 247, 252),
        Color.FromArgb(17, 26, 41),
        Color.FromArgb(58, 72, 92),
        Color.FromArgb(196, 206, 222),
        Color.FromArgb(255, 244, 215),
        Color.FromArgb(50, 39, 19),
        Color.FromArgb(44, 127, 255),
        Color.FromArgb(42, 168, 117),
        Color.FromArgb(223, 230, 241));

    public static UiTheme Dark { get; } = new(
        Color.FromArgb(20, 25, 34),
        Color.FromArgb(29, 35, 46),
        Color.FromArgb(35, 42, 56),
        Color.FromArgb(236, 242, 255),
        Color.FromArgb(174, 187, 209),
        Color.FromArgb(58, 72, 92),
        Color.FromArgb(66, 58, 36),
        Color.FromArgb(246, 229, 181),
        Color.FromArgb(80, 162, 255),
        Color.FromArgb(90, 202, 152),
        Color.FromArgb(53, 64, 81));

    public static UiTheme Blend(UiTheme from, UiTheme to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new UiTheme(
            Lerp(from.WindowBackground, to.WindowBackground, t),
            Lerp(from.Surface, to.Surface, t),
            Lerp(from.SurfaceAlt, to.SurfaceAlt, t),
            Lerp(from.TextPrimary, to.TextPrimary, t),
            Lerp(from.TextSecondary, to.TextSecondary, t),
            Lerp(from.Border, to.Border, t),
            Lerp(from.BannerBackground, to.BannerBackground, t),
            Lerp(from.BannerText, to.BannerText, t),
            Lerp(from.AccentBlue, to.AccentBlue, t),
            Lerp(from.AccentGreen, to.AccentGreen, t),
            Lerp(from.GraphGrid, to.GraphGrid, t));
    }

    private static Color Lerp(Color a, Color b, float t)
    {
        var r = (int)Math.Round(a.R + ((b.R - a.R) * t));
        var g = (int)Math.Round(a.G + ((b.G - a.G) * t));
        var bl = (int)Math.Round(a.B + ((b.B - a.B) * t));
        return Color.FromArgb(r, g, bl);
    }
}
