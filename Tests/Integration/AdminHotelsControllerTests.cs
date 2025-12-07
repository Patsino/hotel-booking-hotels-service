using System.Net;
using System.Net.Http.Json;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class AdminHotelsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AdminHotelsControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient(int? userId = 1, string? role = "Admin")
    {
        return _factory.CreateAuthenticatedClient(userId, role);
    }

    private HttpClient CreateAnonymousClient()
    {
        return _factory.CreateAuthenticatedClient(null, null);
    }

    private async Task<Hotel> SeedHotelAsync(ApprovalStatus approval = ApprovalStatus.Pending)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelsDbContext>();
        
        var hotel = new Hotel(1, "Test Hotel", "USA", "NYC");
        if (approval == ApprovalStatus.Approved)
            hotel.Approve();
        else if (approval == ApprovalStatus.Rejected)
            hotel.Reject();

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();
        return hotel;
    }

    #region GetPending Tests

    [Fact]
    public async Task GetPending_ShouldReturnPendingHotels_WhenAdmin()
    {
        // Arrange
        await SeedHotelAsync(ApprovalStatus.Pending);
        await SeedHotelAsync(ApprovalStatus.Approved);
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.GetAsync("/api/admin/hotels/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPending_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = CreateClient(1, "HotelOwner");

        // Act
        var response = await client.GetAsync("/api/admin/hotels/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPending_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        using var client = CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/api/admin/hotels/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Approve Tests

    [Fact]
    public async Task Approve_ShouldReturnOk_WhenAdminApprovesHotel()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Pending);
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_ShouldReturnBadRequest_WhenHotelAlreadyApproved()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Approved);
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approve_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync("/api/admin/hotels/99999/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Pending);
        using var client = CreateClient(1, "HotelOwner");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Reject Tests

    [Fact]
    public async Task Reject_ShouldReturnOk_WhenAdminRejectsHotel()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Pending);
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_ShouldReturnBadRequest_WhenHotelAlreadyRejected()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Rejected);
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reject_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        using var client = CreateClient(1, "Admin");

        // Act
        var response = await client.PostAsync("/api/admin/hotels/99999/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reject_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var hotel = await SeedHotelAsync(ApprovalStatus.Pending);
        using var client = CreateClient(1, "HotelOwner");

        // Act
        var response = await client.PostAsync($"/api/admin/hotels/{hotel.Id}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
