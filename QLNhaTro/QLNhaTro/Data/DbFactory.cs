using Microsoft.EntityFrameworkCore;
using QLNhaTro.Models;

namespace QLNhaTro.Data;

public static class DbFactory
{
    public static QlNhaTroContext Create()
    {
        var options = new DbContextOptionsBuilder<QlNhaTroContext>()
            .UseSqlServer(Program.Settings.ConnectionStrings.QL_NhaTro)
            .Options;
        return new QlNhaTroContext(options);
    }
}
