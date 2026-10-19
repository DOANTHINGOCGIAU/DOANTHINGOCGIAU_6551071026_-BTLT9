using System.Text;
using Microsoft.Extensions.Configuration;
using QLDaNgoai.Configuration;
using QLDaNgoai.Services;
using Serilog;

namespace QLDaNgoai;

internal static class Program
{
    public static AppSettings Settings { get; private set; } = new();
    public static UserSettings User { get; private set; } = new();

    [STAThread]
    private static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            Log.Error(e.Exception, "Lỗi chưa được xử lý (ThreadException)");
            MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại.\nAn error occurred and has been logged.",
                "Kế hoạch dã ngoại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Lỗi chưa bắt (UnhandledException)");

        try
        {
            // Ngôn ngữ đã lưu từ lần trước
            User = UserSettings.Load();
            Lang.Set(User.Language);

            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .Build();

            Settings = config.Get<AppSettings>() ?? new AppSettings();
            if (string.IsNullOrWhiteSpace(Settings.ConnectionStrings.QL_DaNgoai))
                throw new InvalidOperationException("Thiếu ConnectionStrings:QL_DaNgoai trong appsettings.json");

            Log.Information("Ứng dụng khởi động (môi trường: {Env}, ngôn ngữ: {Lang})", env, Lang.Code);

            ApplicationConfiguration.Initialize();
            Application.Run(new FormChuyenDi());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ứng dụng không khởi động được");
            MessageBox.Show("Ứng dụng không khởi động được. Hãy kiểm tra appsettings.json.\nThe app could not start. Please check appsettings.json.",
                "Kế hoạch dã ngoại", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
