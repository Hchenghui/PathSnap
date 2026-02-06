using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PathSnap;

/// <summary>
/// 设置窗口 - 贴近 modern-screenshot-tool-ui 原型
/// </summary>
public class SettingsForm : Form
{
    private readonly Bitmap _headerAppIconBitmap;
    private readonly TextBox _saveDirTextBox;
    private readonly TextBox _hotkeyTextBox;
    private readonly Label _hotkeyHintLabel;
    private readonly RoundedPanel _recordBadge;
    private readonly Label _recordBadgeText;
    private bool _isRecordingHotkey;
    private bool _ignoreNextMouseCapture;

    public string SaveDir => _saveDirTextBox.Text.Trim();
    public string Hotkey => HotkeyText.Normalize(_hotkeyTextBox.Text);

    public SettingsForm(string saveDir, string hotkey, Icon appIcon)
    {
        Text = "PathSnap - 设置";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(520, 430);
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        Icon = appIcon;
        KeyPreview = true;

        KeyDown += OnFormKeyDown;
        _hotkeyHintLabel = new Label();

        var root = new RoundedPanel
        {
            Radius = 16,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderColor = UiPalette.Border,
            BorderWidth = 1
        };
        Controls.Add(root);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 66,
            BackColor = UiPalette.SurfaceSoft
        };
        header.MouseDown += (_, e) => BeginDrag(e.Button);

        _headerAppIconBitmap = new Bitmap(appIcon.ToBitmap(), new Size(32, 32));
        var appBadge = new PictureBox
        {
            Location = new Point(24, 16),
            Size = new Size(32, 32),
            BackColor = Color.Transparent,
            Image = _headerAppIconBitmap,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        appBadge.MouseDown += (_, e) => BeginDrag(e.Button);
        header.Controls.Add(appBadge);

        var appName = new Label
        {
            Text = "PathSnap",
            AutoSize = true,
            Location = new Point(64, 10),
            ForeColor = UiPalette.TextPrimary,
            Font = new Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold)
        };
        appName.MouseDown += (_, e) => BeginDrag(e.Button);
        header.Controls.Add(appName);

        var subtitle = new Label
        {
            Text = "应用设置",
            AutoSize = true,
            Location = new Point(64, 35),
            ForeColor = UiPalette.TextSecondary,
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        subtitle.MouseDown += (_, e) => BeginDrag(e.Button);
        header.Controls.Add(subtitle);

        var closeButton = new RoundedButton
        {
            IconKind = UiIconKind.Close,
            Radius = 8,
            FillColor = Color.Transparent,
            HoverColor = Color.FromArgb(254, 242, 242),
            BorderColor = Color.Transparent,
            IconColor = Color.FromArgb(148, 163, 184),
            HoverIconColor = UiPalette.Danger,
            Location = new Point(478, 18),
            Size = new Size(30, 30)
        };
        closeButton.Click += (_, _) =>
        {
            if (_isRecordingHotkey)
            {
                _hotkeyHintLabel.Text = "录制中：请先停止监听再关闭窗口";
                return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        };
        header.Controls.Add(closeButton);

        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(241, 245, 249));
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = UiPalette.SurfaceSoft
        };

        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(241, 245, 249));
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var cancelButton = new RoundedButton
        {
            Text = "取消",
            Radius = 9,
            FillColor = Color.White,
            HoverColor = Color.FromArgb(248, 250, 252),
            BorderColor = Color.FromArgb(226, 232, 240),
            TextColor = Color.FromArgb(75, 85, 99),
            Location = new Point(408, 14),
            Size = new Size(88, 36),
            Font = new Font("Microsoft YaHei UI", 9.2F)
        };
        cancelButton.Click += (_, _) =>
        {
            if (_isRecordingHotkey)
            {
                _hotkeyHintLabel.Text = "录制中：请先停止监听再取消";
                return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        };
        footer.Controls.Add(cancelButton);

        var saveButton = new RoundedButton
        {
            Text = "保存并应用",
            IconKind = UiIconKind.Save,
            Radius = 9,
            FillColor = UiPalette.Primary,
            HoverColor = Color.FromArgb(29, 78, 216),
            BorderColor = Color.Transparent,
            TextColor = Color.White,
            IconColor = Color.White,
            HoverIconColor = Color.White,
            Location = new Point(280, 14),
            Size = new Size(118, 36),
            Font = new Font("Microsoft YaHei UI", 9.2F)
        };
        saveButton.Click += (_, _) =>
        {
            if (_isRecordingHotkey)
            {
                StopHotkeyRecording("已停止录制并保存当前快捷键");
            }

            if (!ValidateForm())
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        footer.Controls.Add(saveButton);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
        root.Controls.Add(content);
        root.Controls.Add(footer);
        root.Controls.Add(header);

        var pathTitleIcon = new UiIcon
        {
            IconKind = UiIconKind.Folder,
            Location = new Point(0, 0),
            Size = new Size(15, 15),
            IconColor = UiPalette.Primary
        };

        var pathTitle = new Label
        {
            Text = "图片保存目录",
            AutoSize = true,
            Location = new Point(22, 0),
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
        };

        var pathTitleRow = new Panel
        {
            Location = new Point(24, 20),
            Size = new Size(240, 20)
        };
        pathTitleRow.Controls.Add(pathTitleIcon);
        pathTitleRow.Controls.Add(pathTitle);
        AlignIconWithLabel(pathTitleRow, pathTitleIcon, pathTitle);
        content.Controls.Add(pathTitleRow);

        var pathInputSurface = new RoundedPanel
        {
            Radius = 8,
            BackColor = UiPalette.InputBg,
            BorderColor = UiPalette.InputBorder,
            BorderWidth = 1,
            Location = new Point(24, 50),
            Size = new Size(386, 34)
        };

        _saveDirTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Location = new Point(11, 9),
            Size = new Size(364, 17),
            Text = saveDir,
            BackColor = UiPalette.InputBg,
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 9.3F)
        };
        pathInputSurface.Controls.Add(_saveDirTextBox);
        content.Controls.Add(pathInputSurface);

        var browseButton = new RoundedButton
        {
            Text = "浏览...",
            Radius = 8,
            FillColor = Color.White,
            HoverColor = Color.FromArgb(249, 250, 251),
            BorderColor = UiPalette.InputBorder,
            TextColor = Color.FromArgb(75, 85, 99),
            Location = new Point(418, 50),
            Size = new Size(78, 34),
            Font = new Font("Microsoft YaHei UI", 9F)
        };
        browseButton.Click += (_, _) => ChooseDirectory();
        content.Controls.Add(browseButton);

        var pathHint = new Label
        {
            Text = "截图将自动保存到该文件夹",
            AutoSize = true,
            Location = new Point(26, 91),
            ForeColor = Color.FromArgb(156, 163, 175),
            Font = new Font("Microsoft YaHei UI", 8.7F)
        };
        content.Controls.Add(pathHint);

        var hotkeyTitleIcon = new UiIcon
        {
            IconKind = UiIconKind.Keyboard,
            Location = new Point(0, 0),
            Size = new Size(15, 15),
            IconColor = UiPalette.Primary
        };

        var hotkeyTitle = new Label
        {
            Text = "全局快捷键",
            AutoSize = true,
            Location = new Point(22, 0),
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
        };

        var hotkeyTitleRow = new Panel
        {
            Location = new Point(24, 122),
            Size = new Size(240, 20)
        };
        hotkeyTitleRow.Controls.Add(hotkeyTitleIcon);
        hotkeyTitleRow.Controls.Add(hotkeyTitle);
        AlignIconWithLabel(hotkeyTitleRow, hotkeyTitleIcon, hotkeyTitle);
        content.Controls.Add(hotkeyTitleRow);

        var hotkeyInputSurface = new RoundedPanel
        {
            Radius = 8,
            BackColor = UiPalette.InputBg,
            BorderColor = UiPalette.InputBorder,
            BorderWidth = 1,
            Location = new Point(24, 152),
            Size = new Size(472, 34)
        };

        _hotkeyTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Location = new Point(11, 9),
            Size = new Size(390, 17),
            Text = HotkeyText.FormatForDisplay(hotkey),
            BackColor = UiPalette.InputBg,
            ForeColor = Color.FromArgb(31, 41, 55),
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            ReadOnly = true,
            TabStop = false,
            Cursor = Cursors.Hand
        };
        _hotkeyTextBox.MouseDown += (_, _) => StartHotkeyRecordingByInputClick();
        hotkeyInputSurface.Controls.Add(_hotkeyTextBox);

        _recordBadge = new RoundedPanel
        {
            Radius = 4,
            BackColor = Color.FromArgb(229, 231, 235),
            BorderColor = Color.FromArgb(209, 213, 219),
            BorderWidth = 1,
            Location = new Point(422, 8),
            Size = new Size(38, 18)
        };

        _recordBadgeText = new Label
        {
            Text = "REC",
            AutoSize = true,
            Location = new Point(7, 3),
            ForeColor = Color.FromArgb(107, 114, 128),
            Font = new Font("Segoe UI", 7.2F, FontStyle.Bold)
        };
        _recordBadge.Controls.Add(_recordBadgeText);
        hotkeyInputSurface.Controls.Add(_recordBadge);
        content.Controls.Add(hotkeyInputSurface);

        _hotkeyHintLabel.Text = "点击快捷键输入框开始监听，然后按键盘或鼠标按键完成录制";
        _hotkeyHintLabel.AutoSize = true;
        _hotkeyHintLabel.Location = new Point(26, 193);
        _hotkeyHintLabel.ForeColor = Color.FromArgb(156, 163, 175);
        _hotkeyHintLabel.Font = new Font("Microsoft YaHei UI", 8.7F);
        content.Controls.Add(_hotkeyHintLabel);

        var infoBox = new RoundedPanel
        {
            Radius = 10,
            BackColor = Color.FromArgb(239, 246, 255),
            BorderColor = Color.FromArgb(219, 234, 254),
            BorderWidth = 1,
            Location = new Point(24, 224),
            Size = new Size(472, 42)
        };
        content.Controls.Add(infoBox);

        var infoIcon = new UiIcon
        {
            IconKind = UiIconKind.Info,
            Size = new Size(16, 16),
            Location = new Point(10, 13),
            IconColor = Color.FromArgb(59, 130, 246)
        };
        infoBox.Controls.Add(infoIcon);

        var infoText = new Label
        {
            Text = "所有更改将在点击“保存并应用”后立即生效。程序将继续在系统托盘后台运行。",
            AutoSize = true,
            Location = new Point(34, 12),
            ForeColor = Color.FromArgb(29, 78, 216),
            Font = new Font("Microsoft YaHei UI", 8.8F)
        };
        infoBox.Controls.Add(infoText);
        AlignIconWithLabel(infoBox, infoIcon, infoText);

        AttachHotkeyMouseCaptureHandlers(this);
        UpdateRecordVisual(false);
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Escape)
            {
                StopHotkeyRecording("已取消快捷键录制");
                return;
            }

            if (HotkeyText.IsModifierKey(e.KeyCode))
            {
                _hotkeyHintLabel.Text = "继续按下主键，或按 Esc 取消录制";
                return;
            }

            var normalizedHotkey = HotkeyText.BuildHotkeyString(
                e.Modifiers,
                HotkeyText.NormalizeKeyboardKeyDisplay(e.KeyCode),
                IsWindowsKeyPressed());
            CommitRecordedHotkey(normalizedHotkey);
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void AttachHotkeyMouseCaptureHandlers(Control root)
    {
        root.MouseDown += OnAnyControlMouseDown;
        foreach (Control child in root.Controls)
        {
            AttachHotkeyMouseCaptureHandlers(child);
        }
    }

    private void OnAnyControlMouseDown(object? sender, MouseEventArgs e)
    {
        if (_ignoreNextMouseCapture)
        {
            _ignoreNextMouseCapture = false;
            return;
        }

        if (!_isRecordingHotkey)
        {
            return;
        }

        var mouseKeyToken = e.Button switch
        {
            MouseButtons.Left => "MouseLeft",
            MouseButtons.Right => "MouseRight",
            MouseButtons.Middle => "MouseMiddle",
            MouseButtons.XButton1 => "MouseX1",
            MouseButtons.XButton2 => "MouseX2",
            _ => null
        };

        if (mouseKeyToken == null)
        {
            return;
        }

        var normalizedHotkey = HotkeyText.BuildHotkeyString(
            Control.ModifierKeys,
            mouseKeyToken,
            IsWindowsKeyPressed());
        CommitRecordedHotkey(normalizedHotkey);
    }

    private void StartHotkeyRecordingByInputClick()
    {
        if (_isRecordingHotkey)
        {
            return;
        }

        _ignoreNextMouseCapture = true;
        StartHotkeyRecording();
    }

    private void StartHotkeyRecording()
    {
        _isRecordingHotkey = true;
        UpdateRecordVisual(true);
        _hotkeyHintLabel.Text = "录制中：按下组合键（支持键盘主键或鼠标按键），按 Esc 可取消";
    }

    private void StopHotkeyRecording(string hintText)
    {
        _isRecordingHotkey = false;
        UpdateRecordVisual(false);
        _hotkeyHintLabel.Text = hintText;
    }

    private void CommitRecordedHotkey(string normalizedHotkey)
    {
        _hotkeyTextBox.Text = HotkeyText.FormatForDisplay(normalizedHotkey);
        StopHotkeyRecording($"已录制：{HotkeyText.FormatForDisplay(normalizedHotkey)}");
        ActiveControl = null;
    }

    private void UpdateRecordVisual(bool isRecording)
    {
        _recordBadge.BackColor = isRecording ? Color.FromArgb(254, 226, 226) : Color.FromArgb(229, 231, 235);
        _recordBadge.BorderColor = isRecording ? Color.FromArgb(252, 165, 165) : Color.FromArgb(209, 213, 219);
        _recordBadgeText.ForeColor = isRecording ? Color.FromArgb(185, 28, 28) : Color.FromArgb(107, 114, 128);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsWindowsKeyPressed()
    {
        return (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateWindowRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateWindowRegion();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            StopHotkeyRecording("已取消快捷键录制");
        }

        _headerAppIconBitmap.Dispose();
        base.OnFormClosing(e);
    }

    private void UpdateWindowRegion()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var path = UiGraphics.CreateRoundedPath(new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), 16);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    private void BeginDrag(MouseButtons button)
    {
        if (button != MouseButtons.Left)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(Handle, 0xA1, 0x2, 0);
    }

    private void ChooseDirectory()
    {
        if (_isRecordingHotkey)
        {
            _hotkeyHintLabel.Text = "录制中：请先停止监听再选择目录";
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "选择图片保存目录",
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(SaveDir))
        {
            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(SaveDir));
                if (Directory.Exists(fullPath))
                {
                    dialog.SelectedPath = fullPath;
                }
            }
            catch
            {
                // 忽略无效路径，允许用户重新选择
            }
        }

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        _saveDirTextBox.Text = dialog.SelectedPath.Trim();
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(SaveDir))
        {
            MessageBox.Show("保存目录不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(Hotkey))
        {
            MessageBox.Show("快捷键不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private static void AlignIconWithLabel(Control row, Control icon, Label label)
    {
        label.Top = (row.Height - label.Height) / 2;
        icon.Top = (row.Height - icon.Height) / 2;
        label.Left = icon.Right + 8;
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);
}

internal enum UiIconKind
{
    None,
    App,
    Folder,
    Keyboard,
    Info,
    Save,
    Close
}

internal static class UiPalette
{
    public static readonly Color WindowBackdrop = Color.FromArgb(228, 233, 241);
    public static readonly Color SurfaceSoft = Color.FromArgb(249, 250, 251);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color InputBorder = Color.FromArgb(229, 231, 235);
    public static readonly Color InputBg = Color.FromArgb(249, 250, 251);
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);
    public static readonly Color TextPrimary = Color.FromArgb(31, 41, 55);
    public static readonly Color TextSecondary = Color.FromArgb(107, 114, 128);
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);
}

internal static class UiGraphics
{
    public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var normalizedRadius = Math.Max(1, radius);
        var maxRadius = Math.Min(rect.Width, rect.Height) / 2;
        normalizedRadius = Math.Min(normalizedRadius, Math.Max(1, maxRadius));
        var diameter = normalizedRadius * 2;
        var right = rect.Right;
        var bottom = rect.Bottom;

        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void DrawCenteredString(Graphics g, string text, Font font, Color color, Rectangle bounds)
    {
        TextRenderer.DrawText(
            g,
            text,
            font,
            bounds,
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

internal class RoundedPanel : Panel
{
    public int Radius { get; set; } = 8;
    public Color BorderColor { get; set; } = Color.Transparent;
    public int BorderWidth { get; set; }

    public RoundedPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = UiGraphics.CreateRoundedPath(rect, Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);

        if (BorderWidth > 0)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            e.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = UiGraphics.CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}

internal sealed class RoundedButton : Control
{
    private bool _hovered;

    public int Radius { get; set; } = 8;
    public Color FillColor { get; set; } = Color.White;
    public Color HoverColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Transparent;
    public Color TextColor { get; set; } = Color.Black;
    public Color IconColor { get; set; } = Color.Black;
    public Color HoverIconColor { get; set; } = Color.Black;
    public UiIconKind IconKind { get; set; } = UiIconKind.None;

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        Cursor = Cursors.Hand;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = UiGraphics.CreateRoundedPath(rect, Radius);
        using var brush = new SolidBrush(_hovered ? HoverColor : FillColor);
        e.Graphics.FillPath(brush, path);

        if (BorderColor.A > 0)
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawPath(pen, path);
        }

        var textRect = rect;
        if (IconKind != UiIconKind.None)
        {
            var iconSize = string.IsNullOrWhiteSpace(Text)
                ? (IconKind == UiIconKind.Close ? 15 : 12)
                : 14;
            var iconRect = string.IsNullOrWhiteSpace(Text)
                ? new Rectangle((Width - iconSize) / 2, (Height - iconSize) / 2, iconSize, iconSize)
                : new Rectangle(10, (Height - iconSize) / 2, iconSize, iconSize);
            UiIcon.DrawIcon(e.Graphics, IconKind, iconRect, _hovered ? HoverIconColor : IconColor, true);

            if (!string.IsNullOrWhiteSpace(Text))
            {
                textRect = new Rectangle(30, 0, Width - 30, Height);
            }
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            flags |= IconKind == UiIconKind.None
                ? TextFormatFlags.HorizontalCenter
                : TextFormatFlags.Left;

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                TextColor,
                flags);
        }
    }
}

internal sealed class UiIcon : Control
{
    public UiIconKind IconKind { get; set; } = UiIconKind.None;
    public Color IconColor { get; set; } = Color.White;

    public UiIcon()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        DrawIcon(e.Graphics, IconKind, new Rectangle(0, 0, Width - 1, Height - 1), IconColor, false);
    }

    public static void DrawIcon(Graphics g, UiIconKind kind, Rectangle rect, Color color, bool compact)
    {
        switch (kind)
        {
            case UiIconKind.App:
                DrawApp(g, rect);
                break;
            case UiIconKind.Folder:
                IconProvider.DrawIcon(g, "folder", rect, color);
                break;
            case UiIconKind.Keyboard:
                IconProvider.DrawIcon(g, "keyboard", rect, color);
                break;
            case UiIconKind.Info:
                IconProvider.DrawIcon(g, "info", rect, color);
                break;
            case UiIconKind.Save:
                IconProvider.DrawIcon(g, "save", rect, color);
                break;
            case UiIconKind.Close:
                IconProvider.DrawIcon(g, "x", rect, color);
                break;
        }
    }

    private static void DrawApp(Graphics g, Rectangle rect)
    {
        using var path = UiGraphics.CreateRoundedPath(rect, 7);
        using var bgBrush = new SolidBrush(Color.White);
        g.FillPath(bgBrush, path);

        var strokeColor = Color.FromArgb(0, 134, 230);
        using var framePen = new Pen(strokeColor, Math.Max(1.4F, rect.Width / 9F))
        {
            LineJoin = LineJoin.Round
        };
        var inset = Math.Max(2, rect.Width / 11);
        var innerRect = Rectangle.Inflate(rect, -inset, -inset);
        using var framePath = UiGraphics.CreateRoundedPath(innerRect, Math.Max(3, rect.Width / 5));
        g.DrawPath(framePen, framePath);

        using var routePen = new Pen(strokeColor, Math.Max(1.6F, rect.Width / 8.5F))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        var start = new PointF(rect.X + rect.Width * 0.32F, rect.Y + rect.Height * 0.72F);
        var mid = new PointF(rect.X + rect.Width * 0.52F, rect.Y + rect.Height * 0.52F);
        var end = new PointF(rect.X + rect.Width * 0.72F, rect.Y + rect.Height * 0.34F);
        g.DrawLine(routePen, start, mid);
        g.DrawLine(routePen, mid, end);

        var dotOuter = Math.Max(4F, rect.Width * 0.18F);
        using var dotBrush = new SolidBrush(strokeColor);
        g.FillEllipse(dotBrush, start.X - dotOuter / 2, start.Y - dotOuter / 2, dotOuter, dotOuter);
        var dotInner = dotOuter * 0.46F;
        g.FillEllipse(Brushes.White, start.X - dotInner / 2, start.Y - dotInner / 2, dotInner, dotInner);

        var head = Math.Max(2F, rect.Width * 0.11F);
        g.DrawLine(routePen, end.X, end.Y, end.X - head, end.Y);
        g.DrawLine(routePen, end.X, end.Y, end.X, end.Y + head);
    }
}
