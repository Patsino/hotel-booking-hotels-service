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

    #region SearchRoomsAsync Tests

    [Fact]
    public async Task SearchRoomsAsync_ShouldReturnOnlyVisibleRooms()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var visibleRoom = new Room(hotel.Id, 2, 1, 100m);
        var hiddenRoom = new Room(hotel.Id, 4, 2, 200m);
        hiddenRoom.Hide();

        await _context.Rooms.AddRangeAsync(visibleRoom, hiddenRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(new List<int> { hotel.Id }, null, null, null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Visible.Should().BeTrue();
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByCapacity()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var smallRoom = new Room(hotel.Id, 2, 1, 100m);
        var largeRoom = new Room(hotel.Id, 6, 3, 300m);

        await _context.Rooms.AddRangeAsync(smallRoom, largeRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(new List<int> { hotel.Id }, 4, null, null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Capacity.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByAccommodationType()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var hotelRoom = new Room(hotel.Id, 2, 1, 100m);
        var apartment = new Room(hotel.Id, 4, 2, 200m);
        apartment.Update(null, null, 4, 2, 200m, false, AccommodationType.Apartment);

        await _context.Rooms.AddRangeAsync(hotelRoom, apartment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel.Id }, null, "Apartment", null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Accommodation.Should().Be(AccommodationType.Apartment);
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByMinPrice()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var cheapRoom = new Room(hotel.Id, 2, 1, 50m);
        var expensiveRoom = new Room(hotel.Id, 4, 2, 200m);

        await _context.Rooms.AddRangeAsync(cheapRoom, expensiveRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel.Id }, null, null, 100m, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().PricePerNight.Should().BeGreaterThanOrEqualTo(100m);
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByMaxPrice()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var cheapRoom = new Room(hotel.Id, 2, 1, 50m);
        var expensiveRoom = new Room(hotel.Id, 4, 2, 200m);

        await _context.Rooms.AddRangeAsync(cheapRoom, expensiveRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel.Id }, null, null, null, 100m, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().PricePerNight.Should().BeLessThanOrEqualTo(100m);
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByPetsAllowed()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        var noPetsRoom = new Room(hotel.Id, 2, 1, 100m);
        var petsRoom = new Room(hotel.Id, 4, 2, 200m);
        petsRoom.Update(null, null, 4, 2, 200m, true, AccommodationType.HotelRoom);

        await _context.Rooms.AddRangeAsync(noPetsRoom, petsRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel.Id }, null, null, null, null, true);

        // Assert
        result.Should().HaveCount(1);
        result.First().PetsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldFilterByMultipleHotels()
    {
        // Arrange
        var hotel1 = await CreateHotelAsync(1, "Hotel 1");
        var hotel2 = await CreateHotelAsync(2, "Hotel 2");
        var hotel3 = await CreateHotelAsync(3, "Hotel 3");

        var room1 = new Room(hotel1.Id, 2, 1, 100m);
        var room2 = new Room(hotel2.Id, 2, 1, 100m);
        var room3 = new Room(hotel3.Id, 2, 1, 100m);

        await _context.Rooms.AddRangeAsync(room1, room2, room3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel1.Id, hotel2.Id }, null, null, null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchRoomsAsync_ShouldCombineFilters()
    {
        // Arrange
        var hotel = await CreateHotelAsync();
        
        var matchingRoom = new Room(hotel.Id, 4, 2, 150m);
        matchingRoom.Update(null, null, 4, 2, 150m, true, AccommodationType.Apartment);
        
        var nonMatchingRoom = new Room(hotel.Id, 2, 1, 50m);

        await _context.Rooms.AddRangeAsync(matchingRoom, nonMatchingRoom);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchRoomsAsync(
            new List<int> { hotel.Id }, 3, "Apartment", 100m, 200m, true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().BeEquivalentTo(matchingRoom, options => 
            options.Excluding(r => r.CreatedAt));
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
