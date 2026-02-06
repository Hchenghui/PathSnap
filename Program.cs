using System.Diagnostics;

namespace PathSnap;

/// <summary>
/// 主窗体 - 隐藏窗口，仅用于托盘图标和热键处理
/// </summary>
public class MainForm : Form
{
    private const string AppDisplayName = "PathSnap";

    private readonly NotifyIcon _trayIcon;
    private ContextMenuStrip _contextMenu;
    private readonly HotkeyManager _hotkeyManager;
    private Config _config;
    private bool _resourcesDisposed;

    public MainForm()
    {
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;

        _config = Config.Load();

        _hotkeyManager = new HotkeyManager(Handle, _config.Hotkey);
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        _hotkeyManager.Register();

        _contextMenu = CreateContextMenu();

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = BuildTrayText(),
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _trayIcon.DoubleClick += (_, _) => OpenSettings();
    }

    /// <summary>
    /// 加载托盘和程序图标
    /// </summary>
    private Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        if (!File.Exists(iconPath))
        {
            iconPath = Path.Combine(Application.StartupPath, "Resources", "app.ico");
        }

        if (File.Exists(iconPath))
        {
            try
            {
                return new Icon(iconPath);
            }
            catch
            {
                // ignore and fallback
            }
        }

        return SystemIcons.Application;
    }

    /// <summary>
    /// 创建托盘菜单（现代样式）
    /// </summary>
    private ContextMenuStrip CreateContextMenu()
    {
        var menuWidth = TrayLayout.ContentWidth + 12;
        var menu = new ContextMenuStrip
        {
            Renderer = new TrayMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            BackColor = TrayTheme.Surface,
            Font = new Font("Segoe UI", 9F),
            Padding = new Padding(6),
            MinimumSize = new Size(menuWidth, 0),
            MaximumSize = new Size(menuWidth, int.MaxValue)
        };
        menu.Opening += (_, _) => menu.Width = menuWidth;

        menu.Items.Add(CreateControlHost(new TrayHeaderControl(), new Padding(0, 0, 0, 6)));
        menu.Items.Add(CreateActionHost("打开保存目录", TrayActionIconKind.Folder, OpenSaveDirectory));
        menu.Items.Add(CreateActionHost("设置...", TrayActionIconKind.Settings, OpenSettings));
        menu.Items.Add(CreateControlHost(
            new TrayInfoCardControl
            {
                SavePath = _config.SaveDir,
                Hotkey = _hotkeyManager.CurrentHotkey
            },
            new Padding(0, 6, 0, 6)));
        menu.Items.Add(CreateActionHost("退出程序", TrayActionIconKind.Exit, ExitApplication, isDanger: true, showChevron: true));

        return menu;
    }

    private ToolStripControlHost CreateActionHost(
        string text,
        TrayActionIconKind iconKind,
        Action onClick,
        bool isDanger = false,
        bool showChevron = false)
    {
        var action = new TrayActionControl(text, iconKind, isDanger, showChevron);
        action.ActionInvoked += (_, _) => InvokeMenuAction(onClick);
        return CreateControlHost(action, new Padding(0, 1, 0, 1));
    }

    private static ToolStripControlHost CreateControlHost(Control control, Padding margin)
    {
        return new ToolStripControlHost(control)
        {
            AutoSize = false,
            Size = control.Size,
            Margin = margin,
            Padding = Padding.Empty
        };
    }

    private void InvokeMenuAction(Action action)
    {
        _trayIcon.ContextMenuStrip?.Close(ToolStripDropDownCloseReason.ItemClicked);
        action();
    }

    /// <summary>
    /// 热键触发处理
    /// </summary>
    private void OnHotkeyPressed()
    {
        if (!ClipboardHelper.ContainsImage())
        {
            ShowNotification("剪贴板中无图片", ToolTipIcon.Warning);
            return;
        }

        var image = ClipboardHelper.GetImage();
        if (image == null)
        {
            ShowNotification("无法读取剪贴板图片", ToolTipIcon.Error);
            return;
        }

        var result = ImageSaver.Save(image, _config);
        image.Dispose();

        if (!result.Success)
        {
            ShowNotification(result.ErrorMessage ?? "保存失败", ToolTipIcon.Error);
            return;
        }

        if (ClipboardHelper.SetQuotedPath(result.FilePath!))
        {
            ShowNotification($"已保存并复制路径\n{Path.GetFileName(result.FilePath)}", ToolTipIcon.Info);
        }
        else
        {
            ShowNotification("图片已保存，但复制路径失败", ToolTipIcon.Warning);
        }
    }

    /// <summary>
    /// 打开保存目录
    /// </summary>
    private void OpenSaveDirectory()
    {
        try
        {
            if (!Directory.Exists(_config.SaveDir))
            {
                Directory.CreateDirectory(_config.SaveDir);
            }

            Process.Start("explorer.exe", _config.SaveDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开目录: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    private void OpenSettings()
    {
        using var settingsForm = new SettingsForm(
            _config.SaveDir,
            _hotkeyManager.CurrentHotkey,
            LoadIcon()
        );

        if (settingsForm.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var newSaveDir = settingsForm.SaveDir;
        var newHotkey = settingsForm.Hotkey;
        var saveDirChanged = !string.Equals(_config.SaveDir, newSaveDir, StringComparison.OrdinalIgnoreCase);
        var hotkeyChanged = !string.Equals(_hotkeyManager.CurrentHotkey, newHotkey, StringComparison.OrdinalIgnoreCase);

        if (!saveDirChanged && !hotkeyChanged)
        {
            return;
        }

        // 先确保热键更新成功，再提交其他配置，避免出现“部分生效”。
        if (hotkeyChanged && !_hotkeyManager.UpdateHotkey(newHotkey))
        {
            return;
        }

        _config.SaveDir = newSaveDir;
        _config.Hotkey = _hotkeyManager.CurrentHotkey;
        _config.Save();
        UpdateContextMenu();
        UpdateTrayText();
        ShowNotification($"设置已更新\n热键：{_config.Hotkey}", ToolTipIcon.Info);
    }

    /// <summary>
    /// 更新托盘菜单
    /// </summary>
    private void UpdateContextMenu()
    {
        var oldMenu = _contextMenu;
        _contextMenu = CreateContextMenu();
        _trayIcon.ContextMenuStrip = _contextMenu;
        oldMenu.Dispose();
    }

    /// <summary>
    /// 更新托盘文本
    /// </summary>
    private void UpdateTrayText()
    {
        _trayIcon.Text = BuildTrayText();
    }

    /// <summary>
    /// 生成托盘文本（NotifyIcon 文本最大长度约 63）
    /// </summary>
    private string BuildTrayText()
    {
        var text = $"{AppDisplayName} ({_hotkeyManager.CurrentHotkey})";
        return text.Length <= 63 ? text : AppDisplayName;
    }

    /// <summary>
    /// 显示托盘通知
    /// </summary>
    private void ShowNotification(string message, ToolTipIcon icon)
    {
        _trayIcon.ShowBalloonTip(2200, AppDisplayName, message, icon);
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    private void ExitApplication()
    {
        DisposeRuntimeResources();
        Application.Exit();
    }

    /// <summary>
    /// 处理 Windows 消息
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        _hotkeyManager?.ProcessMessage(ref m);
        base.WndProc(ref m);
    }

    /// <summary>
    /// 窗体关闭时清理资源
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        DisposeRuntimeResources();
        base.OnFormClosing(e);
    }

    private void DisposeRuntimeResources()
    {
        if (_resourcesDisposed)
        {
            return;
        }

        _resourcesDisposed = true;
        _hotkeyManager.Dispose();
        _contextMenu.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}

/// <summary>
/// 程序入口
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "PathSnap_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("PathSnap 已在运行中", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
