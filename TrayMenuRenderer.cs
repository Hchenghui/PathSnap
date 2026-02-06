using System.Drawing.Drawing2D;

namespace PathSnap;

internal static class TrayTheme
{
    public static readonly Color Surface = Color.FromArgb(255, 255, 255);
    public static readonly Color SurfaceSoft = Color.FromArgb(249, 250, 251);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color Hover = Color.FromArgb(239, 246, 255);
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimarySoft = Color.FromArgb(219, 234, 254);
    public static readonly Color Success = Color.FromArgb(34, 197, 94);
    public static readonly Color TextPrimary = Color.FromArgb(55, 65, 81);
    public static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerSoft = Color.FromArgb(254, 242, 242);
}

internal static class TrayLayout
{
    public const int ContentWidth = 244;
}

internal enum TrayActionIconKind
{
    Folder,
    Settings,
    Exit
}

internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    public TrayMenuRenderer() : base(new TrayMenuColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(TrayTheme.Surface);
        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height));
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);

        using var path = TrayGraphics.CreateRoundedPath(rect, 12);
        using var pen = new Pen(TrayTheme.Border);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
    }
}

internal sealed class TrayMenuColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => TrayTheme.Surface;
    public override Color MenuBorder => TrayTheme.Border;
    public override Color MenuItemSelected => Color.Transparent;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color ImageMarginGradientBegin => TrayTheme.Surface;
    public override Color ImageMarginGradientMiddle => TrayTheme.Surface;
    public override Color ImageMarginGradientEnd => TrayTheme.Surface;
}

internal static class TrayGraphics
{
    public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var normalizedRadius = Math.Max(1, radius);
        var diameter = normalizedRadius * 2;

        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter - 1, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter - 1, rect.Bottom - diameter - 1, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter - 1, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static string ShortenPath(string path, int maxLength = 36)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= maxLength)
        {
            return path;
        }

        var headLength = maxLength / 2 - 2;
        var tailLength = maxLength - headLength - 3;
        return path[..headLength] + "..." + path[^tailLength..];
    }
}

internal sealed class TrayHeaderControl : Control
{
    public TrayHeaderControl()
    {
        Size = new Size(TrayLayout.ContentWidth, 42);
        Cursor = Cursors.Default;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bgBrush = new SolidBrush(TrayTheme.SurfaceSoft))
        {
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);
        }

        using (var borderPen = new Pen(TrayTheme.Border))
        {
            e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
        }

        using (var titleFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                e.Graphics,
                "PathSnap",
                titleFont,
                new Rectangle(12, 0, 120, Height - 1),
                Color.FromArgb(107, 114, 128),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        var dotRect = new Rectangle(Width - 20, (Height - 8) / 2, 8, 8);
        using (var glowBrush = new SolidBrush(Color.FromArgb(120, 34, 197, 94)))
        {
            e.Graphics.FillEllipse(glowBrush, dotRect.X - 3, dotRect.Y - 3, 14, 14);
        }

        using (var dotBrush = new SolidBrush(TrayTheme.Success))
        {
            e.Graphics.FillEllipse(dotBrush, dotRect);
        }
    }
}

internal sealed class TrayActionControl : Control
{
    private readonly bool _isDanger;
    private readonly bool _showChevron;
    private readonly TrayActionIconKind _iconKind;
    private bool _hovered;

    public string ActionText { get; }

    public event EventHandler? ActionInvoked;

    public TrayActionControl(string text, TrayActionIconKind iconKind, bool isDanger = false, bool showChevron = false)
    {
        ActionText = text;
        _iconKind = iconKind;
        _isDanger = isDanger;
        _showChevron = showChevron;

        Size = new Size(TrayLayout.ContentWidth, 42);
        Cursor = Cursors.Hand;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ActionInvoked?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_hovered)
        {
            var hoverRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var hoverPath = TrayGraphics.CreateRoundedPath(hoverRect, 8);
            using var hoverBrush = new SolidBrush(_isDanger ? TrayTheme.DangerSoft : TrayTheme.Hover);
            e.Graphics.FillPath(hoverBrush, hoverPath);
        }

        var iconRect = new Rectangle(10, 7, 28, 28);
        using (var iconBgBrush = new SolidBrush(_isDanger
                   ? Color.FromArgb(254, 242, 242)
                   : (_hovered ? TrayTheme.PrimarySoft : Color.FromArgb(243, 244, 246))))
        {
            using var iconBgPath = TrayGraphics.CreateRoundedPath(iconRect, 6);
            e.Graphics.FillPath(iconBgBrush, iconBgPath);
        }

        var iconColor = _isDanger
            ? (_hovered ? TrayTheme.Danger : Color.FromArgb(239, 68, 68))
            : (_hovered ? TrayTheme.Primary : Color.FromArgb(107, 114, 128));
        var glyphRect = new Rectangle(iconRect.X + 6, iconRect.Y + 6, 16, 16);

        switch (_iconKind)
        {
            case TrayActionIconKind.Folder:
                IconProvider.DrawIcon(e.Graphics, "folder", glyphRect, iconColor);
                break;
            case TrayActionIconKind.Settings:
                IconProvider.DrawIcon(e.Graphics, "sliders-horizontal", glyphRect, iconColor);
                break;
            case TrayActionIconKind.Exit:
                IconProvider.DrawIcon(e.Graphics, "log-out", glyphRect, iconColor);
                break;
        }

        var textColor = _isDanger
            ? TrayTheme.Danger
            : (_hovered ? TrayTheme.Primary : TrayTheme.TextPrimary);

        using (var actionFont = new Font("Microsoft YaHei UI", 9.3F, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                e.Graphics,
                ActionText,
                actionFont,
                new Rectangle(48, 0, Width - 56, Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        if (_showChevron)
        {
            var chevronColor = Color.FromArgb(252, 165, 165);
            IconProvider.DrawIcon(e.Graphics, "chevron-right", new Rectangle(Width - 15, 13, 10, 14), chevronColor);
        }
    }
}

internal sealed class TrayInfoCardControl : Control
{
    public string SavePath { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;

    public TrayInfoCardControl()
    {
        Size = new Size(TrayLayout.ContentWidth, 84);
        Cursor = Cursors.Default;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (var borderPen = new Pen(TrayTheme.Border))
        {
            e.Graphics.DrawLine(borderPen, 10, 0, Width - 10, 0);
            e.Graphics.DrawLine(borderPen, 10, Height - 1, Width - 10, Height - 1);
        }

        using var titleFont = new Font("Segoe UI", 7F, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            "当前路径",
            titleFont,
            new Rectangle(12, 6, 80, 12),
            TrayTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

        var pathBox = new Rectangle(12, 20, Width - 24, 22);
        using (var pathBg = new SolidBrush(Color.FromArgb(249, 250, 251)))
        using (var pathBorder = new Pen(Color.FromArgb(243, 244, 246)))
        using (var pathRect = TrayGraphics.CreateRoundedPath(pathBox, 5))
        {
            e.Graphics.FillPath(pathBg, pathRect);
            e.Graphics.DrawPath(pathBorder, pathRect);
        }

        using var pathFont = new Font("Consolas", 8.2F);
        TextRenderer.DrawText(
            e.Graphics,
            TrayGraphics.ShortenPath(SavePath),
            pathFont,
            new Rectangle(pathBox.X + 8, pathBox.Y, pathBox.Width - 14, pathBox.Height),
            Color.FromArgb(75, 85, 99),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

        TextRenderer.DrawText(
            e.Graphics,
            "快捷键",
            titleFont,
            new Rectangle(12, 51, 80, 12),
            TrayTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

        var badgeText = string.IsNullOrWhiteSpace(Hotkey) ? "未设置" : Hotkey;
        using var badgeFont = new Font("Consolas", 8.2F, FontStyle.Bold);
        var badgeSize = TextRenderer.MeasureText(e.Graphics, badgeText, badgeFont, new Size(200, 18), TextFormatFlags.SingleLine);
        var badgeRect = new Rectangle(Width - badgeSize.Width - 14, 48, badgeSize.Width + 4, 20);

        using (var badgeBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
        using (var badgeBorder = new Pen(Color.FromArgb(229, 231, 235)))
        using (var badgePath = TrayGraphics.CreateRoundedPath(badgeRect, 5))
        {
            e.Graphics.FillPath(badgeBg, badgePath);
            e.Graphics.DrawPath(badgeBorder, badgePath);
        }

        TextRenderer.DrawText(
            e.Graphics,
            badgeText,
            badgeFont,
            badgeRect,
            Color.FromArgb(55, 65, 81),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
