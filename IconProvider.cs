using Svg;
using System.Collections.Concurrent;

namespace PathSnap;

internal static class IconProvider
{
    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

    public static Bitmap GetBitmap(string iconName, int size, Color color)
    {
        var key = $"{iconName}|{size}|{color.ToArgb()}";
        return Cache.GetOrAdd(key, _ => RenderBitmap(iconName, size, color));
    }

    public static void DrawIcon(Graphics g, string iconName, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var size = Math.Min(bounds.Width, bounds.Height);
        var icon = GetBitmap(iconName, size, color);

        var x = bounds.X + ((bounds.Width - size) / 2);
        var y = bounds.Y + ((bounds.Height - size) / 2);
        g.DrawImage(icon, x, y, size, size);
    }

    private static Bitmap RenderBitmap(string iconName, int size, Color color)
    {
        var svgTemplate = LoadIconTemplate(iconName);
        if (svgTemplate == null)
        {
            return new Bitmap(size, size);
        }

        var svgText = svgTemplate.Replace("%COLOR%", ColorToHex(color), StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgText));
        var svgDoc = SvgDocument.Open<SvgDocument>(stream);
        svgDoc.Width = size;
        svgDoc.Height = size;

        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);
        svgDoc.Draw(graphics);
        return bitmap;
    }

    private static string? LoadIconTemplate(string iconName)
    {
        var resourceName = $"PathSnap.Resources.Icons.{iconName}.svg";
        using (var stream = typeof(IconProvider).Assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        var iconPath = ResolveIconPath(iconName);
        return iconPath == null ? null : File.ReadAllText(iconPath);
    }

    private static string? ResolveIconPath(string iconName)
    {
        var relativePath = Path.Combine("Resources", "Icons", $"{iconName}.svg");

        var basePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(basePath))
        {
            return basePath;
        }

        var startupPath = Path.Combine(Application.StartupPath, relativePath);
        return File.Exists(startupPath) ? startupPath : null;
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
