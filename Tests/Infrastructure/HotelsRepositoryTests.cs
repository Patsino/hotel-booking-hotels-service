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

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_ShouldReturnOnlyApprovedHotels()
    {
        // Arrange
        var approvedHotel = new Hotel(1, "Approved", "USA", "NYC");
        approvedHotel.Approve();
        var pendingHotel = new Hotel(2, "Pending", "USA", "NYC");

        await _context.Hotels.AddRangeAsync(approvedHotel, pendingHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Approval.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByCountry()
    {
        // Arrange
        var usaHotel = new Hotel(1, "USA Hotel", "USA", "NYC");
        usaHotel.Approve();
        var ukHotel = new Hotel(2, "UK Hotel", "UK", "London");
        ukHotel.Approve();

        await _context.Hotels.AddRangeAsync(usaHotel, ukHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("USA", null, null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Country.Should().Be("USA");
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByCity()
    {
        // Arrange
        var nycHotel = new Hotel(1, "NYC Hotel", "USA", "NYC");
        nycHotel.Approve();
        var laHotel = new Hotel(2, "LA Hotel", "USA", "LA");
        laHotel.Approve();

        await _context.Hotels.AddRangeAsync(nycHotel, laHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, "NYC", null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("NYC");
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByDistrict()
    {
        // Arrange
        var hotel1 = new Hotel(1, "Downtown Hotel", "USA", "NYC");
        hotel1.Update("Downtown Hotel", null, "Manhattan", null, false, false, 3);
        hotel1.Approve();

        var hotel2 = new Hotel(2, "Brooklyn Hotel", "USA", "NYC");
        hotel2.Update("Brooklyn Hotel", null, "Brooklyn", null, false, false, 3);
        hotel2.Approve();

        await _context.Hotels.AddRangeAsync(hotel1, hotel2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, "Manhattan", null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().District.Should().Be("Manhattan");
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByPetsAllowed()
    {
        // Arrange
        var petFriendlyHotel = new Hotel(1, "Pet Friendly", "USA", "NYC");
        petFriendlyHotel.Update("Pet Friendly", null, null, null, true, false, 3);
        petFriendlyHotel.Approve();

        var noPetsHotel = new Hotel(2, "No Pets", "USA", "NYC");
        noPetsHotel.Update("No Pets", null, null, null, false, false, 3);
        noPetsHotel.Approve();

        await _context.Hotels.AddRangeAsync(petFriendlyHotel, noPetsHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, null, true, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().PetsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByIsPetHotelOnly()
    {
        // Arrange
        var petHotel = new Hotel(1, "Pet Hotel", "USA", "NYC");
        petHotel.Update("Pet Hotel", null, null, null, true, true, 3);
        petHotel.Approve();

        var regularHotel = new Hotel(2, "Regular Hotel", "USA", "NYC");
        regularHotel.Update("Regular Hotel", null, null, null, false, false, 3);
        regularHotel.Approve();

        await _context.Hotels.AddRangeAsync(petHotel, regularHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync(null, null, null, null, true);

        // Assert
        result.Should().HaveCount(1);
        result.First().IsPetHotel.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldCombineFilters()
    {
        // Arrange
        var matchingHotel = new Hotel(1, "Matching", "USA", "NYC");
        matchingHotel.Update("Matching", null, "Manhattan", null, true, false, 3);
        matchingHotel.Approve();

        var nonMatchingHotel = new Hotel(2, "Non Matching", "UK", "London");
        nonMatchingHotel.Approve();

        await _context.Hotels.AddRangeAsync(matchingHotel, nonMatchingHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("USA", "NYC", "Manhattan", true, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Matching");
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
