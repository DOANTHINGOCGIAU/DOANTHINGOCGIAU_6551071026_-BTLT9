namespace QLChiTieu.Configuration;

/// <summary>Lớp cấu hình có kiểu (strongly-typed) ánh xạ với appsettings.json.</summary>
public class AppSettings
{
    public ConnectionStringsSection ConnectionStrings { get; set; } = new();
}

public class ConnectionStringsSection
{
    public string QL_ChiTieu { get; set; } = "";
}
