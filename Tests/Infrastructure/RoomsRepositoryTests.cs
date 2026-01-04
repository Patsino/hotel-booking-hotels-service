using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Infrastructure;

public class RoomsRepositoryTests : IDisposable
{
    private readonly HotelsDbContext _context;
    private readonly RoomsRepository _repository;

    public RoomsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HotelsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HotelsDbContext(options);
        _repository = new RoomsRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Hotel> CreateHotelAsync(int ownerId = 1, string name = "Test Hotel")
    {
        var hotel = new Hotel(ownerId, name, "USA", "NYC");
        await _context.Hotels.AddAsync(hotel);
        await _context.SaveChangesAsync();
        return hotel;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRoom_WhenExists()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var room = new Room(hotel.Id, 2, 1, 100m);
        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(room.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Capacity.Should().Be(2);
        result.Bedrooms.Should().Be(1);
        result.PricePerNight.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByHotelIdAsync Tests

    [Fact]
    public async Task GetByHotelIdAsync_ShouldReturnVisibleRooms_WhenIncludeHiddenIsFalse()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var visibleRoom = new Room(hotel.Id, 2, 1, 100m);
        var hiddenRoom = new Room(hotel.Id, 4, 2, 200m);
        hiddenRoom.Hide();

        await _context.Rooms.AddRangeAsync(visibleRoom, hiddenRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByHotelIdAsync(hotel.Id, includeHidden: false);

        // Assert
        result.Should().HaveCount(1);
        result.First().Visible.Should().BeTrue();
    }

    [Fact]
    public async Task GetByHotelIdAsync_ShouldReturnAllRooms_WhenIncludeHiddenIsTrue()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var visibleRoom = new Room(hotel.Id, 2, 1, 100m);
        var hiddenRoom = new Room(hotel.Id, 4, 2, 200m);
        hiddenRoom.Hide();

        await _context.Rooms.AddRangeAsync(visibleRoom, hiddenRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByHotelIdAsync(hotel.Id, includeHidden: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByHotelIdAsync_ShouldReturnEmpty_WhenNoRoomsExist()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        // Act
        var result = await _repository.GetByHotelIdAsync(hotel.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddRoom()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var room = new Room(hotel.Id, 2, 1, 100m);

        // Act
        await _repository.AddAsync(room);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _context.Rooms.FirstOrDefaultAsync(r => r.HotelId == hotel.Id);
        result.Should().NotBeNull();
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var room = new Room(hotel.Id, 2, 1, 100m);
        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();

        // Act
        room.Hide();
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _context.Rooms.FindAsync(room.Id);
        result!.Visible.Should().BeFalse();
    }

    #endregion
}
