using System.Drawing.Imaging;

namespace PathSnap;

/// <summary>
/// 图片保存结果
/// </summary>
public class SaveResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 图片保存器 - 处理图片的保存逻辑
/// </summary>
public static class ImageSaver
{
    /// <summary>
    /// 保存图片到指定目录
    /// </summary>
    public static SaveResult Save(Image image, Config config)
    {
        try
        {
            // 确保保存目录存在
            if (!Directory.Exists(config.SaveDir))
            {
                try
                {
                    Directory.CreateDirectory(config.SaveDir);
                }
                catch (Exception ex)
                {
                    return new SaveResult
                    {
                        Success = false,
                        ErrorMessage = $"无法创建目录: {ex.Message}"
                    };
                }
            }

            // 生成文件名
            var fileName = GenerateFileName(config);
            var filePath = Path.Combine(config.SaveDir, fileName);

            // 确保文件名不冲突
            filePath = EnsureUniqueFileName(filePath);

            // 保存图片
            var format = GetImageFormat(config.ImageFormat);
            image.Save(filePath, format);

            return new SaveResult
            {
                Success = true,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            return new SaveResult
            {
                Success = false,
                ErrorMessage = $"保存失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 生成文件名
    /// </summary>
    private static string GenerateFileName(Config config)
    {
        var timestamp = DateTime.Now.ToString(config.FileNamePattern);
        return $"{timestamp}.{config.ImageFormat}";
    }

    /// <summary>
    /// 确保文件名唯一
    /// </summary>
    private static string EnsureUniqueFileName(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath)!;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var counter = 1;
        string newPath;

        do
        {
            newPath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// 获取图片格式
    /// </summary>
    private static ImageFormat GetImageFormat(string format)
    {
        return format.ToLower() switch
        {
            "jpg" or "jpeg" => ImageFormat.Jpeg,
            "bmp" => ImageFormat.Bmp,
            "gif" => ImageFormat.Gif,
            _ => ImageFormat.Png
        };
    }
}
