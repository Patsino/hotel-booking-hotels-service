using HotelBooking.Hotels.Domain.Hotels;

namespace Tests.Domain;

public class HotelTests
{
    [Fact]
    public void Constructor_ShouldCreateHotel_WithValidParameters()
    {
        // Arrange
        var ownerId = 1;
        var name = "Test Hotel";
        var country = "USA";
        var city = "New York";

        // Act
        var hotel = new Hotel(ownerId, name, country, city);

        // Assert
        hotel.OwnerId.Should().Be(ownerId);
        hotel.Name.Should().Be(name);
        hotel.Country.Should().Be(country);
        hotel.City.Should().Be(city);
        hotel.CancelFreeDaysBefore.Should().Be(3); // Default value
        hotel.Approval.Should().Be(ApprovalStatus.Pending);
        hotel.SubmittedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var hotel = new Hotel(1, "Test", "Country", "City");

        // Assert
        hotel.Description.Should().BeNull();
        hotel.MainImageUrl.Should().BeNull();
        hotel.District.Should().BeNull();
        hotel.AddressLine.Should().BeNull();
        hotel.PetsAllowed.Should().BeFalse();
        hotel.IsPetHotel.Should().BeFalse();
        hotel.ReviewedAt.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var hotel = new Hotel(1, "Original", "Country", "City");
        var newName = "Updated Hotel";
        var newDescription = "Updated Description";
        var newDistrict = "Downtown";
        var newAddressLine = "123 Main St";
        var newPetsAllowed = true;
        var newIsPetHotel = true;
        var newCancelFreeDaysBefore = 7;

        // Act
        hotel.Update(newName, newDescription, newDistrict, newAddressLine, 
            newPetsAllowed, newIsPetHotel, newCancelFreeDaysBefore);

        // Assert
        hotel.Name.Should().Be(newName);
        hotel.Description.Should().Be(newDescription);
        hotel.District.Should().Be(newDistrict);
        hotel.AddressLine.Should().Be(newAddressLine);
        hotel.PetsAllowed.Should().BeTrue();
        hotel.IsPetHotel.Should().BeTrue();
        hotel.CancelFreeDaysBefore.Should().Be(newCancelFreeDaysBefore);
    }

    [Fact]
    public void Update_ShouldAllowNullOptionalValues()
    {
        // Arrange
        var hotel = new Hotel(1, "Original", "Country", "City");

        // Act
        hotel.Update("Name", null, null, null, false, false, 1);

        // Assert
        hotel.Description.Should().BeNull();
        hotel.District.Should().BeNull();
        hotel.AddressLine.Should().BeNull();
    }

    [Fact]
    public void Submit_ShouldSetPendingStatus_AndUpdateSubmittedAt()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "Country", "City");
        hotel.Approve(); // First approve to change status
        var originalSubmittedAt = hotel.SubmittedAt;

        // Act
        hotel.Submit();

        // Assert
        hotel.Approval.Should().Be(ApprovalStatus.Pending);
        hotel.SubmittedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Approve_ShouldSetApprovedStatus_AndReviewedAt()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "Country", "City");

        // Act
        hotel.Approve();

        // Assert
        hotel.Approval.Should().Be(ApprovalStatus.Approved);
        hotel.ReviewedAt.Should().NotBeNull();
        hotel.ReviewedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reject_ShouldSetRejectedStatus_AndReviewedAt()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "Country", "City");

        // Act
        hotel.Reject();

        // Assert
        hotel.Approval.Should().Be(ApprovalStatus.Rejected);
        hotel.ReviewedAt.Should().NotBeNull();
        hotel.ReviewedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("Hotel A", "USA", "New York")]
    [InlineData("Hotel B", "UK", "London")]
    [InlineData("Hotel C", "France", "Paris")]
    public void Constructor_ShouldAcceptDifferentLocations(string name, string country, string city)
    {
        // Act
        var hotel = new Hotel(1, name, country, city);

        // Assert
        hotel.Name.Should().Be(name);
        hotel.Country.Should().Be(country);
        hotel.City.Should().Be(city);
    }

    [Fact]
    public void ApprovalStatusTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "Country", "City");
        hotel.Approval.Should().Be(ApprovalStatus.Pending);

        // Act & Assert - Approve
        hotel.Approve();
        hotel.Approval.Should().Be(ApprovalStatus.Approved);

        // Act & Assert - Submit again (resubmit)
        hotel.Submit();
        hotel.Approval.Should().Be(ApprovalStatus.Pending);

        // Act & Assert - Reject
        hotel.Reject();
        hotel.Approval.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Update_ShouldNotChangeOwnerIdOrCountryOrCity()
    {
        // Arrange
        var originalOwnerId = 1;
        var originalCountry = "USA";
        var originalCity = "New York";
        var hotel = new Hotel(originalOwnerId, "Test", originalCountry, originalCity);

        // Act
        hotel.Update("New Name", "Description", "District", "Address", true, true, 5);

        // Assert - These should remain unchanged
        hotel.OwnerId.Should().Be(originalOwnerId);
        hotel.Country.Should().Be(originalCountry);
        hotel.City.Should().Be(originalCity);
    }
}
