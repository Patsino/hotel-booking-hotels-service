using System.Net;
using System.Net.Http.Json;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class RoomsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public RoomsControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient(int? userId = 1, string? role = "HotelOwner")
    {
        return _factory.CreateAuthenticatedClient(userId, role);
    }

    private HttpClient CreateAnonymousClient()
    {
        return _factory.CreateAuthenticatedClient(null, null);
    }

    private async Task<Hotel> SeedHotelAsync(int ownerId = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelsDbContext>();
        
        var hotel = new Hotel(ownerId, "Test Hotel", "USA", "NYC");
        hotel.Approve();
        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> SeedRoomAsync(int hotelId, bool visible = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelsDbContext>();
        
        var room = new Room(hotelId, 2, 1, 100m);
        if (!visible)
            room.Hide();
        context.Rooms.Add(room);
        await context.SaveChangesAsync();
        return room;
    }

    #region Create Tests

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ownerId: 1);
        using var client = CreateClient(1, "HotelOwner");

        var command = new
        {
            HotelId = hotel.Id,
            Capacity = 4,
            Bedrooms = 2,
            PricePerNight = 150.00m,
            RoomNumber = "101",
            Description = "Deluxe Room",
            PetsAllowed = true,
            Accommodation = "HotelRoom"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/rooms", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenHotelDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");
        var command = new
        {
            HotelId = 99999,
            Capacity = 2,
            Bedrooms = 1,
            PricePerNight = 100m,
            Accommodation = "HotelRoom"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/rooms", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnRoom_WhenVisible()
    {
        // Arrange
        var hotel = await SeedHotelAsync();
        var room = await SeedRoomAsync(hotel.Id, visible: true);
        using var client = CreateAnonymousClient();

        // Act
        var response = await client.GetAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenHiddenAndNotOwner()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ownerId: 1);
        var room = await SeedRoomAsync(hotel.Id, visible: false);
        using var client = CreateClient(999, "User"); // Different user

        // Act
        var response = await client.GetAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturnRoom_WhenOwnerViewsHidden()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId);
        var room = await SeedRoomAsync(hotel.Id, visible: false);
        using var client = CreateClient(ownerId, "HotelOwner");

        // Act
        var response = await client.GetAsync($"/api/rooms/{room.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/rooms/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenOwnerUpdates()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId);
        var room = await SeedRoomAsync(hotel.Id);
        using var client = CreateClient(ownerId, "HotelOwner");

        var updateCommand = new
        {
            RoomNumber = "202",
            Description = "Updated description",
            Capacity = 4,
            Bedrooms = 2,
            PricePerNight = 200m,
            PetsAllowed = true,
            Accommodation = "Apartment"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/rooms/{room.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");
        var updateCommand = new
        {
            RoomNumber = "202",
            Capacity = 4,
            Bedrooms = 2,
            PricePerNight = 200m,
            Accommodation = "HotelRoom"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/rooms/99999", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Hide/Show Tests

    [Fact]
    public async Task Hide_ShouldReturnNoContent_WhenOwnerHides()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId);
        var room = await SeedRoomAsync(hotel.Id);
        using var client = CreateClient(ownerId, "HotelOwner");

        // Act
        var response = await client.PostAsync($"/api/rooms/{room.Id}/hide", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Show_ShouldReturnNoContent_WhenOwnerShows()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId);
        var room = await SeedRoomAsync(hotel.Id, visible: false);
        using var client = CreateClient(ownerId, "HotelOwner");

        // Act
        var response = await client.PostAsync($"/api/rooms/{room.Id}/show", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Hide_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");

        // Act
        var response = await client.PostAsync("/api/rooms/99999/hide", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
