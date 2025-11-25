using HotelBooking.Hotels.Domain.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Hotels.Infrastructure.Persistence;

public sealed class HotelsDbContext : DbContext
{
    public HotelsDbContext(DbContextOptions<HotelsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hotel> Hotels => Set<Hotel>();

    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("hotels");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HotelsDbContext).Assembly);
    }
}
