using System.Net;
using System.Net.Http.Json;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class HotelsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public HotelsControllerTests()
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

    private async Task<Hotel> SeedHotelAsync(int ownerId = 1, string name = "Test Hotel", 
        ApprovalStatus approval = ApprovalStatus.Pending)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelsDbContext>();
        
        var hotel = new Hotel(ownerId, name, "USA", "NYC");
        if (approval == ApprovalStatus.Approved)
            hotel.Approve();
        else if (approval == ApprovalStatus.Rejected)
            hotel.Reject();

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();
        return hotel;
    }

    #region Create Tests

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");
        var command = new
        {
            OwnerId = 1,
            Name = "New Hotel",
            Country = "USA",
            City = "New York",
            Description = "A nice hotel",
            PetsAllowed = true,
            IsPetHotel = false,
            CancelFreeDaysBefore = 5
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/hotels", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenCreatingForDifferentOwner()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");
        var command = new
        {
            OwnerId = 999, // Different owner
            Name = "New Hotel",
            Country = "USA",
            City = "New York"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/hotels", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldAllowAdmin_ToCreateForAnyOwner()
    {
        // Arrange
        using var client = CreateClient(1, "Admin");
        var command = new
        {
            OwnerId = 999, // Different owner, but admin can do this
            Name = "New Hotel",
            Country = "USA",
            City = "New York"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/hotels", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnHotel_WhenApproved()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var hotel = await SeedHotelAsync(approval: ApprovalStatus.Approved);

        // Act
        var response = await client.GetAsync($"/api/hotels/{hotel.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPendingAndNotOwner()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ownerId: 1, approval: ApprovalStatus.Pending);
        using var client = CreateClient(999, "User"); // Different user

        // Act
        var response = await client.GetAsync($"/api/hotels/{hotel.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturnHotel_WhenOwnerViewsPending()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId, approval: ApprovalStatus.Pending);
        using var client = CreateClient(ownerId, "HotelOwner");

        // Act
        var response = await client.GetAsync($"/api/hotels/{hotel.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/hotels/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetMine Tests

    [Fact]
    public async Task GetMine_ShouldReturnOwnerHotels()
    {
        // Arrange
        var ownerId = 1;
        await SeedHotelAsync(ownerId: ownerId, name: "My Hotel 1");
        await SeedHotelAsync(ownerId: ownerId, name: "My Hotel 2");
        await SeedHotelAsync(ownerId: 999, name: "Other Hotel"); // Different owner
        using var client = CreateClient(ownerId, "HotelOwner");

        // Act
        var response = await client.GetAsync("/api/hotels/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hotels = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        hotels.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMine_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        using var client = CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/api/hotels/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenOwnerUpdates()
    {
        // Arrange
        var ownerId = 1;
        var hotel = await SeedHotelAsync(ownerId: ownerId);
        using var client = CreateClient(ownerId, "HotelOwner");

        var updateCommand = new
        {
            Name = "Updated Hotel",
            Description = "Updated description",
            PetsAllowed = true,
            IsPetHotel = false,
            CancelFreeDaysBefore = 7
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/hotels/{hotel.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");
        var updateCommand = new
        {
            Name = "Updated Hotel",
            CancelFreeDaysBefore = 7
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/hotels/99999", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

}

