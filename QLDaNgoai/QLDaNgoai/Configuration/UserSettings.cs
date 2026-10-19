using System.Text.Json;
using Serilog;

namespace QLDaNgoai.Configuration;

/// <summary>Cài đặt riêng của người dùng, lưu ở %LocalAppData%\KeHoachDaNgoai\usersettings.json</summary>
public sealed class UserSettings
{
    public string Language { get; set; } = "vi";

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeHoachDaNgoai", "usersettings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new UserSettings();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Không đọc được usersettings.json, dùng mặc định");
        }
        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Không lưu được usersettings.json");
        }
    }
}
