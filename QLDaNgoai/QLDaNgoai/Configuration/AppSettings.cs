namespace QLDaNgoai.Configuration;

public class AppSettings
{
    public ConnectionStringsSection ConnectionStrings { get; set; } = new();
}

public class ConnectionStringsSection
{
    public string QL_DaNgoai { get; set; } = "";
}
