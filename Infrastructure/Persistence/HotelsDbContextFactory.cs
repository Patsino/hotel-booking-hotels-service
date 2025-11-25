using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelBooking.Hotels.Infrastructure.Persistence;

public sealed class HotelsDbContextFactory : IDesignTimeDbContextFactory<HotelsDbContext>
{
    public HotelsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__HotelsDatabase");

        var optionsBuilder = new DbContextOptionsBuilder<HotelsDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "hotels");
        });

        return new HotelsDbContext(optionsBuilder.Options);
    }
}
