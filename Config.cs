using System.Text.Json;

namespace PathSnap;

/// <summary>
/// 配置管理类 - 处理应用配置的读写和持久化
/// </summary>
public class Config
{
    private const string CurrentAppDataFolder = "PathSnap";
    private const string DefaultImageFormat = "png";
    private const string DefaultFileNamePattern = "yyyyMMdd_HHmmss_fff";
    private const string DefaultHotkey = "Ctrl+Shift+V";
    private static readonly string[] LegacyAppDataFolders = ["ImagePathMate", "ScreenshotPathTool"];
    private static readonly string DefaultSaveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "Screenshots"
    );

    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        CurrentAppDataFolder
    );

    private static readonly string ConfigFilePath = Path.Combine(AppDataPath, "config.json");
    private static readonly string[] LegacyConfigFilePaths = LegacyAppDataFolders
        .Select(folder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            folder,
            "config.json"))
        .ToArray();

    /// <summary>
    /// 图片保存目录
    /// </summary>
    public string SaveDir { get; set; } = DefaultSaveDir;

    /// <summary>
    /// 图片格式
    /// </summary>
    public string ImageFormat { get; set; } = DefaultImageFormat;

    /// <summary>
    /// 文件名模式
    /// </summary>
    public string FileNamePattern { get; set; } = DefaultFileNamePattern;

    /// <summary>
    /// 全局快捷键（示例：Ctrl+Shift+V）
    /// </summary>
    public string Hotkey { get; set; } = DefaultHotkey;

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    public static Config Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<Config>(json);
                return Normalize(config);
            }

            foreach (var legacyConfigFilePath in LegacyConfigFilePaths)
            {
                if (!File.Exists(legacyConfigFilePath))
                {
                    continue;
                }

                var json = File.ReadAllText(legacyConfigFilePath);
                var config = Normalize(JsonSerializer.Deserialize<Config>(json));
                config.Save();
                return config;
            }
        }
        catch
        {
            // 加载失败时返回默认配置
        }

        return new Config();
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public void Save()
    {
        try
        {
            // 确保目录存在
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Config Normalize(Config? config)
    {
        var normalized = config ?? new Config();

        if (string.IsNullOrWhiteSpace(normalized.SaveDir))
        {
            normalized.SaveDir = DefaultSaveDir;
        }
        else
        {
            normalized.SaveDir = normalized.SaveDir.Trim();
        }

        normalized.ImageFormat = NormalizeImageFormat(normalized.ImageFormat);

        if (string.IsNullOrWhiteSpace(normalized.FileNamePattern))
        {
            normalized.FileNamePattern = DefaultFileNamePattern;
        }
        else
        {
            normalized.FileNamePattern = normalized.FileNamePattern.Trim();
            try
            {
                _ = DateTime.Now.ToString(normalized.FileNamePattern);
            }
            catch (FormatException)
            {
                normalized.FileNamePattern = DefaultFileNamePattern;
            }
        }

        if (string.IsNullOrWhiteSpace(normalized.Hotkey))
        {
            normalized.Hotkey = DefaultHotkey;
        }
        else
        {
            normalized.Hotkey = HotkeyText.Normalize(normalized.Hotkey);
            if (string.IsNullOrWhiteSpace(normalized.Hotkey))
            {
                normalized.Hotkey = DefaultHotkey;
            }
        }

        return normalized;
    }

    private static string NormalizeImageFormat(string? imageFormat)
    {
        if (string.IsNullOrWhiteSpace(imageFormat))
        {
            return DefaultImageFormat;
        }

        return imageFormat.Trim().ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "jpg",
            "bmp" => "bmp",
            "gif" => "gif",
            _ => DefaultImageFormat
        };
    }
}
