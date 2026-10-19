using Microsoft.EntityFrameworkCore;
using QLDaNgoai.Models;

namespace QLDaNgoai.Data;

public static class DbFactory
{
    public static QlDaNgoaiContext Create()
    {
        var options = new DbContextOptionsBuilder<QlDaNgoaiContext>()
            .UseSqlServer(Program.Settings.ConnectionStrings.QL_DaNgoai)
            .Options;
        return new QlDaNgoaiContext(options);
    }
}
