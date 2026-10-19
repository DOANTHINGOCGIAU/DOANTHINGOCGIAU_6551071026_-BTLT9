using Microsoft.EntityFrameworkCore;
using QLChiTieu.Models;

namespace QLChiTieu.Data;

/// <summary>Tạo DbContext từ connection string trong appsettings.json (không hard-code).</summary>
public static class DbFactory
{
    public static QlChiTieuContext Create()
    {
        var options = new DbContextOptionsBuilder<QlChiTieuContext>()
            .UseSqlServer(Program.Settings.ConnectionStrings.QL_ChiTieu)
            .Options;
        return new QlChiTieuContext(options);
    }
}
