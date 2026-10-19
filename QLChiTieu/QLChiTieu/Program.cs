using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using QLChiTieu.Configuration;
using Serilog;

namespace QLChiTieu;

internal static class Program
{
    /// <summary>Cấu hình đã đọc từ appsettings.json (dùng chung toàn ứng dụng).</summary>
    public static AppSettings Settings { get; private set; } = new();

    [STAThread]
    private static void Main()
    {
        // Định dạng ngày / số theo Việt Nam: 18/10/2026, 1.250.000
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        CultureInfo.CurrentCulture = vi;
        CultureInfo.CurrentUICulture = vi;

        // Serilog: ghi file theo ngày vào thư mục Logs cạnh file .exe  ->  Logs\log-yyyyMMdd.txt
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .Build();

            Settings = config.Get<AppSettings>() ?? new AppSettings();

            if (string.IsNullOrWhiteSpace(Settings.ConnectionStrings.QL_ChiTieu))
                throw new InvalidOperationException("Thiếu ConnectionStrings:QL_ChiTieu trong appsettings.json");

            Log.Information("Ứng dụng khởi động (môi trường: {Env})", env);

            ApplicationConfiguration.Initialize();
            Application.Run(new FormChiTieu());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ứng dụng không khởi động được");
            MessageBox.Show(
                "Ứng dụng không khởi động được.\nVui lòng kiểm tra file appsettings.json (chuỗi kết nối CSDL).\n\n" +
                "Chi tiết đã được ghi vào thư mục Logs.",
                "Sổ chi tiêu cá nhân", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("Ứng dụng đóng");
            Log.CloseAndFlush();
        }
    }
}
