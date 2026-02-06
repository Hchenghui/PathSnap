namespace PathSnap;

/// <summary>
/// 剪贴板助手 - 处理剪贴板中的图片读取和文本写入
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// 检查剪贴板中是否包含图片
    /// </summary>
    public static bool ContainsImage()
    {
        try
        {
            return Clipboard.ContainsImage();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从剪贴板获取图片
    /// </summary>
    public static Image? GetImage()
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                return Clipboard.GetImage();
            }
        }
        catch
        {
            // 剪贴板访问失败
        }

        return null;
    }

    /// <summary>
    /// 将文本设置到剪贴板
    /// </summary>
    public static bool SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 将带双引号的路径设置到剪贴板
    /// </summary>
    public static bool SetQuotedPath(string path)
    {
        return SetText($"\"{path}\"");
    }
}
