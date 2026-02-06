using System.Runtime.InteropServices;

namespace PathSnap;

/// <summary>
/// 全局热键管理器 - 键盘组合键使用 RegisterHotKey，鼠标组合键使用低级鼠标钩子
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int HOTKEY_ID = 9000;

    public const int WM_HOTKEY = 0x0312;

    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    private const int XBUTTON1 = 1;
    private const int XBUTTON2 = 2;

    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_SHIFT = 0x10;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private readonly IntPtr _handle;
    private readonly LowLevelMouseProc _mouseHookProc;

    private bool _isRegistered;
    private uint _modifiers;
    private uint _virtualKey;
    private HotkeyMainInputKind _mainInputKind;
    private HotkeyMouseButton _mouseButton;
    private IntPtr _mouseHookHandle = IntPtr.Zero;

    /// <summary>
    /// 热键触发事件
    /// </summary>
    public event Action? HotkeyPressed;

    /// <summary>
    /// 当前热键显示文本
    /// </summary>
    public string CurrentHotkey { get; private set; } = "Ctrl+Shift+V";

    public HotkeyManager(IntPtr windowHandle, string hotkey)
    {
        _handle = windowHandle;
        _mouseHookProc = MouseHookCallback;

        if (!TryParseHotkey(hotkey, out var definition, out _))
        {
            TryParseHotkey("Ctrl+Shift+V", out definition, out _);
        }

        ApplyDefinition(definition);
        CurrentHotkey = definition.NormalizedHotkey;
    }

    /// <summary>
    /// 注册全局热键
    /// </summary>
    public bool Register()
    {
        if (_isRegistered)
        {
            return true;
        }

        if (_mainInputKind == HotkeyMainInputKind.Keyboard)
        {
            _isRegistered = RegisterHotKey(
                _handle,
                HOTKEY_ID,
                _modifiers | MOD_NOREPEAT,
                _virtualKey
            );
        }
        else
        {
            _mouseHookHandle = InstallMouseHook();
            _isRegistered = _mouseHookHandle != IntPtr.Zero;
        }

        if (!_isRegistered)
        {
            MessageBox.Show(
                $"无法注册热键 {CurrentHotkey}，可能已被其他程序占用。",
                "热键注册失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        return _isRegistered;
    }

    /// <summary>
    /// 更新并重新注册热键
    /// </summary>
    public bool UpdateHotkey(string hotkey)
    {
        if (!TryParseHotkey(hotkey, out var newDefinition, out var error))
        {
            MessageBox.Show(
                $"快捷键格式无效：{error}\n示例：F8、Ctrl+Shift+V、Ctrl+Alt+S、MouseX1",
                "快捷键设置失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return false;
        }

        var oldDefinition = new HotkeyDefinition(
            _modifiers,
            _virtualKey,
            CurrentHotkey,
            _mainInputKind,
            _mouseButton);
        var wasRegistered = _isRegistered;

        if (wasRegistered)
        {
            Unregister();
        }

        ApplyDefinition(newDefinition);
        CurrentHotkey = newDefinition.NormalizedHotkey;

        if (!Register())
        {
            ApplyDefinition(oldDefinition);
            CurrentHotkey = oldDefinition.NormalizedHotkey;

            if (wasRegistered)
            {
                Register();
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 处理 Windows 消息，检测键盘热键触发
    /// </summary>
    public void ProcessMessage(ref Message m)
    {
        if (_mainInputKind == HotkeyMainInputKind.Keyboard &&
            m.Msg == WM_HOTKEY &&
            m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
        }
    }

    /// <summary>
    /// 注销热键
    /// </summary>
    public void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        if (_mainInputKind == HotkeyMainInputKind.Keyboard)
        {
            UnregisterHotKey(_handle, HOTKEY_ID);
        }
        else if (_mouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }

        _isRegistered = false;
    }

    public void Dispose()
    {
        Unregister();
    }

    private void ApplyDefinition(HotkeyDefinition definition)
    {
        _modifiers = definition.Modifiers;
        _virtualKey = definition.VirtualKey;
        _mainInputKind = definition.MainInputKind;
        _mouseButton = definition.MouseButton;
    }

    private IntPtr InstallMouseHook()
    {
        var moduleHandle = GetModuleHandle(null);
        return SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, moduleHandle, 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            _isRegistered &&
            _mainInputKind == HotkeyMainInputKind.Mouse &&
            TryGetMouseButton((int)wParam, lParam, out var button) &&
            button == _mouseButton &&
            IsModifierStateMatched(_modifiers))
        {
            HotkeyPressed?.Invoke();
        }

        return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private static bool TryGetMouseButton(int message, IntPtr lParam, out HotkeyMouseButton mouseButton)
    {
        mouseButton = HotkeyMouseButton.Left;

        switch (message)
        {
            case WM_LBUTTONDOWN:
                mouseButton = HotkeyMouseButton.Left;
                return true;
            case WM_RBUTTONDOWN:
                mouseButton = HotkeyMouseButton.Right;
                return true;
            case WM_MBUTTONDOWN:
                mouseButton = HotkeyMouseButton.Middle;
                return true;
            case WM_XBUTTONDOWN:
                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var highWord = (info.mouseData >> 16) & 0xFFFF;
                if (highWord == XBUTTON1)
                {
                    mouseButton = HotkeyMouseButton.X1;
                    return true;
                }

                if (highWord == XBUTTON2)
                {
                    mouseButton = HotkeyMouseButton.X2;
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool IsModifierStateMatched(uint modifiers)
    {
        var ctrlPressed = IsPressed(VK_CONTROL);
        var altPressed = IsPressed(VK_MENU);
        var shiftPressed = IsPressed(VK_SHIFT);
        var winPressed = IsPressed(VK_LWIN) || IsPressed(VK_RWIN);

        var ctrlRequired = (modifiers & MOD_CONTROL) != 0;
        var altRequired = (modifiers & MOD_ALT) != 0;
        var shiftRequired = (modifiers & MOD_SHIFT) != 0;
        var winRequired = (modifiers & MOD_WIN) != 0;

        return ctrlPressed == ctrlRequired &&
               altPressed == altRequired &&
               shiftPressed == shiftRequired &&
               winPressed == winRequired;
    }

    private static bool IsPressed(int keyCode)
    {
        return (GetAsyncKeyState(keyCode) & 0x8000) != 0;
    }

    private static bool TryParseHotkey(
        string hotkey,
        out HotkeyDefinition definition,
        out string error)
    {
        definition = default;
        error = "";

        if (string.IsNullOrWhiteSpace(hotkey))
        {
            error = "不能为空";
            return false;
        }

        var parts = hotkey
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 1)
        {
            error = "至少需要一个主键";
            return false;
        }

        uint modifiers = 0;
        string? keyToken = null;

        foreach (var part in parts)
        {
            var token = part.Trim();
            switch (token.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= MOD_ALT;
                    break;
                case "shift":
                    modifiers |= MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    if (keyToken != null)
                    {
                        error = "只能设置一个主键";
                        return false;
                    }

                    keyToken = token;
                    break;
            }
        }

        if (keyToken == null)
        {
            error = "缺少主键";
            return false;
        }

        var normalizedParts = BuildNormalizedModifierParts(modifiers);

        if (TryParseMouseButton(keyToken, out var mouseButton, out var mouseDisplay))
        {
            normalizedParts.Add(mouseDisplay);
            definition = new HotkeyDefinition(
                modifiers,
                0,
                string.Join("+", normalizedParts),
                HotkeyMainInputKind.Mouse,
                mouseButton);
            return true;
        }

        var keyTokenForParse = keyToken;
        if (keyTokenForParse.Length == 1 && char.IsDigit(keyTokenForParse[0]))
        {
            keyTokenForParse = $"D{keyTokenForParse}";
        }

        if (!Enum.TryParse<Keys>(keyTokenForParse, true, out var key))
        {
            error = $"不支持的主键：{keyToken}";
            return false;
        }

        if (HotkeyText.IsModifierKey(key))
        {
            error = "主键不能是 Ctrl/Alt/Shift/Win";
            return false;
        }

        normalizedParts.Add(HotkeyText.NormalizeKeyboardKeyDisplay(key));
        definition = new HotkeyDefinition(
            modifiers,
            (uint)key,
            string.Join("+", normalizedParts),
            HotkeyMainInputKind.Keyboard,
            HotkeyMouseButton.Left);
        return true;
    }

    private static List<string> BuildNormalizedModifierParts(uint modifiers)
    {
        var parts = new List<string>(4);
        if ((modifiers & MOD_CONTROL) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & MOD_ALT) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & MOD_SHIFT) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & MOD_WIN) != 0)
        {
            parts.Add("Win");
        }

        return parts;
    }

    private static bool TryParseMouseButton(string token, out HotkeyMouseButton button, out string display)
    {
        button = HotkeyMouseButton.Left;
        display = "";
        var normalized = token.Trim().Replace("-", "", StringComparison.OrdinalIgnoreCase).Replace("_", "", StringComparison.OrdinalIgnoreCase);
        normalized = normalized.ToLowerInvariant();

        switch (normalized)
        {
            case "mouseleft":
            case "leftmouse":
            case "lbutton":
                button = HotkeyMouseButton.Left;
                display = "MouseLeft";
                return true;
            case "mouseright":
            case "rightmouse":
            case "rbutton":
                button = HotkeyMouseButton.Right;
                display = "MouseRight";
                return true;
            case "mousemiddle":
            case "middlemouse":
            case "mbutton":
                button = HotkeyMouseButton.Middle;
                display = "MouseMiddle";
                return true;
            case "mousex1":
            case "xbutton1":
                button = HotkeyMouseButton.X1;
                display = "MouseX1";
                return true;
            case "mousex2":
            case "xbutton2":
                button = HotkeyMouseButton.X2;
                display = "MouseX2";
                return true;
            default:
                return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private readonly struct HotkeyDefinition
    {
        public HotkeyDefinition(
            uint modifiers,
            uint virtualKey,
            string normalizedHotkey,
            HotkeyMainInputKind mainInputKind,
            HotkeyMouseButton mouseButton)
        {
            Modifiers = modifiers;
            VirtualKey = virtualKey;
            NormalizedHotkey = normalizedHotkey;
            MainInputKind = mainInputKind;
            MouseButton = mouseButton;
        }

        public uint Modifiers { get; }
        public uint VirtualKey { get; }
        public string NormalizedHotkey { get; }
        public HotkeyMainInputKind MainInputKind { get; }
        public HotkeyMouseButton MouseButton { get; }
    }

    private enum HotkeyMainInputKind
    {
        Keyboard,
        Mouse
    }

    private enum HotkeyMouseButton
    {
        Left,
        Right,
        Middle,
        X1,
        X2
    }
}
