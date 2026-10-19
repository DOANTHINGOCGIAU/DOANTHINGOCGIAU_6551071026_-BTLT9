using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using QLNhaTro.Configuration;
using Serilog;

namespace QLNhaTro;

internal static class Program
{
    public static AppSettings Settings { get; private set; } = new();

    [STAThread]
    private static void Main()
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        CultureInfo.CurrentCulture = vi;
        CultureInfo.CurrentUICulture = vi;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // ----- Xử lý lỗi toàn cục (phải đăng ký trước khi tạo form) -----
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => BaoLoi(e.Exception, "Application.ThreadException");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Lỗi chưa bắt (AppDomain.UnhandledException), IsTerminating={IsTerminating}", e.IsTerminating);
            MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại.", "Quản lý nhà trọ",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (e.IsTerminating) Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Lỗi task không được quan sát (UnobservedTaskException)");
            e.SetObserved();
        };

        try
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .Build();

            Settings = config.Get<AppSettings>() ?? new AppSettings();
            if (string.IsNullOrWhiteSpace(Settings.ConnectionStrings.QL_NhaTro))
                throw new InvalidOperationException("Thiếu ConnectionStrings:QL_NhaTro trong appsettings.json");

            Log.Information("Ứng dụng khởi động (môi trường: {Env})", env);

            ApplicationConfiguration.Initialize();
            Application.Run(new FormKhachThue());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ứng dụng không khởi động được");
            MessageBox.Show(
                "Ứng dụng không khởi động được.\nVui lòng kiểm tra file appsettings.json (chuỗi kết nối CSDL).",
                "Quản lý nhà trọ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("Ứng dụng đóng");
            Log.CloseAndFlush();
        }
    }

    private static void BaoLoi(Exception ex, string nguon)
    {
        Log.Error(ex, "Lỗi chưa được xử lý ({Nguon})", nguon);   // ghi cả stack trace
        MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại.\nỨng dụng vẫn tiếp tục chạy.",
            "Quản lý nhà trọ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
