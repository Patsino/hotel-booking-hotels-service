using Application.Commands;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Infrastructure;

public class HotelsRepositoryTests : IDisposable
{
    private readonly HotelsDbContext _context;
    private readonly HotelsRepository _repository;

    public HotelsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HotelsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HotelsDbContext(options);
        _repository = new HotelsRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnHotel_WhenExists()
    {
        // Arrange
        var hotel = new Hotel(1, "Test Hotel", "USA", "NYC");
        await _context.Hotels.AddAsync(hotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(hotel.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Hotel");
        result.Country.Should().Be("USA");
        result.City.Should().Be("NYC");
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

    #region GetByOwnerIdAsync Tests

    [Fact]
    public async Task GetByOwnerIdAsync_ShouldReturnHotels_ForOwner()
    {
        // Arrange
        var ownerId = 5;
        var hotel1 = new Hotel(ownerId, "Hotel 1", "USA", "NYC");
        var hotel2 = new Hotel(ownerId, "Hotel 2", "USA", "LA");
        var hotel3 = new Hotel(10, "Other Owner Hotel", "USA", "Chicago");

        await _context.Hotels.AddRangeAsync(hotel1, hotel2, hotel3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByOwnerIdAsync(ownerId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(h => h.OwnerId.Should().Be(ownerId));
    }

    [Fact]
    public async Task GetByOwnerIdAsync_ShouldReturnEmpty_WhenNoHotels()
    {
        // Act
        var result = await _repository.GetByOwnerIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPendingAsync Tests

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingHotels()
    {
        // Arrange
        var pendingHotel1 = new Hotel(1, "Pending 1", "USA", "NYC");
        var pendingHotel2 = new Hotel(2, "Pending 2", "USA", "LA");
        var approvedHotel = new Hotel(3, "Approved", "USA", "Chicago");
        approvedHotel.Approve();
        var rejectedHotel = new Hotel(4, "Rejected", "USA", "Miami");
        rejectedHotel.Reject();

        await _context.Hotels.AddRangeAsync(pendingHotel1, pendingHotel2, approvedHotel, rejectedHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(h => h.Approval.Should().Be(ApprovalStatus.Pending));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllHotels_OrderedBySubmittedAt()
    {
        // Arrange
        var hotel1 = new Hotel(1, "Hotel 1", "USA", "NYC");
        var hotel2 = new Hotel(2, "Hotel 2", "USA", "LA");
        var hotel3 = new Hotel(3, "Hotel 3", "USA", "Chicago");

        await _context.Hotels.AddRangeAsync(hotel1, hotel2, hotel3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region SearchHotelsWithRoomsAsync Tests

    [Fact]
    public async Task SearchHotelsWithRoomsAsync_ShouldReturnOnlyApprovedHotels()
    {
        // Arrange
        var approvedHotel = new Hotel(1, "Approved", "USA", "NYC");
        approvedHotel.Approve();
        var room1 = new Room(approvedHotel.Id, 2, 1, 100m);
        
        var pendingHotel = new Hotel(2, "Pending", "USA", "NYC");
        var room2 = new Room(pendingHotel.Id, 2, 1, 100m);

        await _context.Hotels.AddRangeAsync(approvedHotel, pendingHotel);
        await _context.Rooms.AddRangeAsync(room1, room2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchHotelsWithRoomsAsync(new SearchHotelsQuery());

        // Assert
        result.Should().HaveCount(1);
        result.First().HotelName.Should().Be("Approved");
    }

    [Fact]
    public async Task SearchHotelsWithRoomsAsync_ShouldFilterByCountry()
    {
        // Arrange
        var usaHotel = new Hotel(1, "USA Hotel", "USA", "NYC");
        usaHotel.Approve();
        var room1 = new Room(usaHotel.Id, 2, 1, 100m);
        
        var ukHotel = new Hotel(2, "UK Hotel", "UK", "London");
        ukHotel.Approve();
        var room2 = new Room(ukHotel.Id, 2, 1, 100m);

        await _context.Hotels.AddRangeAsync(usaHotel, ukHotel);
        await _context.Rooms.AddRangeAsync(room1, room2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchHotelsWithRoomsAsync(new SearchHotelsQuery(Country: "USA"));

        // Assert
        result.Should().HaveCount(1);
        result.First().Country.Should().Be("USA");
    }

    [Fact]
    public async Task SearchHotelsWithRoomsAsync_ShouldFilterByCity()
    {
        // Arrange
        var nycHotel = new Hotel(1, "NYC Hotel", "USA", "NYC");
        nycHotel.Approve();
        var room1 = new Room(nycHotel.Id, 2, 1, 100m);
        
        var laHotel = new Hotel(2, "LA Hotel", "USA", "LA");
        laHotel.Approve();
        var room2 = new Room(laHotel.Id, 2, 1, 100m);

        await _context.Hotels.AddRangeAsync(nycHotel, laHotel);
        await _context.Rooms.AddRangeAsync(room1, room2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchHotelsWithRoomsAsync(new SearchHotelsQuery(City: "NYC"));

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("NYC");
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddHotel()
    {
        // Arrange
        var hotel = new Hotel(1, "New Hotel", "USA", "NYC");

        // Act
        await _repository.AddAsync(hotel);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _context.Hotels.FirstOrDefaultAsync(h => h.Name == "New Hotel");
        result.Should().NotBeNull();
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var hotel = new Hotel(1, "Test Hotel", "USA", "NYC");
        await _context.Hotels.AddAsync(hotel);
        await _context.SaveChangesAsync();

        // Act
        hotel.Approve();
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _context.Hotels.FindAsync(hotel.Id);
        result!.Approval.Should().Be(ApprovalStatus.Approved);
    }

    #endregion
}
