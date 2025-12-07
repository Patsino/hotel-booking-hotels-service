using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace HotelBooking.Hotels.Infrastructure.Persistence;

[ExcludeFromCodeCoverage]
public sealed class HotelsDbContextFactory : IDesignTimeDbContextFactory<HotelsDbContext>
{
    public HotelsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__HotelBookingDatabase");

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			connectionString = "Server=(localdb)\\mssqllocaldb;Database=HotelBooking;Trusted_Connection=True;TrustServerCertificate=True;";
		}

        var optionsBuilder = new DbContextOptionsBuilder<HotelsDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "hotels");
        });

        return new HotelsDbContext(optionsBuilder.Options);
    }
}
