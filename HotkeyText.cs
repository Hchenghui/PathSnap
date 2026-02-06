namespace PathSnap;

internal static class HotkeyText
{
    public static string Normalize(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.Empty;
        }

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join("+", parts);
    }

    public static string FormatForDisplay(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.Empty;
        }

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" + ", parts);
    }

    public static bool IsModifierKey(Keys key)
    {
        return key is Keys.ControlKey or
            Keys.Menu or
            Keys.ShiftKey or
            Keys.LWin or
            Keys.RWin;
    }

    public static string NormalizeKeyboardKeyDisplay(Keys key)
    {
        var name = key.ToString();
        if (name.StartsWith("D", StringComparison.Ordinal) &&
            name.Length == 2 &&
            char.IsDigit(name[1]))
        {
            return name[1].ToString();
        }

        return name.ToUpperInvariant();
    }

    public static string BuildHotkeyString(Keys modifiers, string mainKeyToken, bool includeWindowsKey)
    {
        var parts = new List<string>(5);

        if ((modifiers & Keys.Control) == Keys.Control)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & Keys.Alt) == Keys.Alt)
        {
            parts.Add("Alt");
        }

        if ((modifiers & Keys.Shift) == Keys.Shift)
        {
            parts.Add("Shift");
        }

        if (includeWindowsKey)
        {
            parts.Add("Win");
        }

        parts.Add(mainKeyToken);
        return string.Join("+", parts);
    }
}
