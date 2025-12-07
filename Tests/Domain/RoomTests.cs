using HotelBooking.Hotels.Domain.Hotels;

namespace Tests.Domain;

public class RoomTests
{
    [Fact]
    public void Constructor_ShouldCreateRoom_WithValidParameters()
    {
        // Arrange
        var hotelId = 1;
        var capacity = 4;
        var bedrooms = 2;
        var pricePerNight = 150.00m;

        // Act
        var room = new Room(hotelId, capacity, bedrooms, pricePerNight);

        // Assert
        room.HotelId.Should().Be(hotelId);
        room.Capacity.Should().Be(capacity);
        room.Bedrooms.Should().Be(bedrooms);
        room.PricePerNight.Should().Be(pricePerNight);
        room.Visible.Should().BeTrue(); // Default value
        room.Accommodation.Should().Be(AccommodationType.HotelRoom); // Default value
        room.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var room = new Room(1, 2, 1, 100m);

        // Assert
        room.RoomNumber.Should().BeNull();
        room.Description.Should().BeNull();
        room.MainImageUrl.Should().BeNull();
        room.PetsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);
        var newRoomNumber = "101A";
        var newDescription = "Deluxe Suite";
        var newCapacity = 6;
        var newBedrooms = 3;
        var newPricePerNight = 250.00m;
        var newPetsAllowed = true;
        var newAccommodation = AccommodationType.Apartment;

        // Act
        room.Update(newRoomNumber, newDescription, newCapacity, newBedrooms, 
            newPricePerNight, newPetsAllowed, newAccommodation);

        // Assert
        room.RoomNumber.Should().Be(newRoomNumber);
        room.Description.Should().Be(newDescription);
        room.Capacity.Should().Be(newCapacity);
        room.Bedrooms.Should().Be(newBedrooms);
        room.PricePerNight.Should().Be(newPricePerNight);
        room.PetsAllowed.Should().BeTrue();
        room.Accommodation.Should().Be(AccommodationType.Apartment);
    }

    [Fact]
    public void Update_ShouldAllowNullOptionalValues()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);

        // Act
        room.Update(null, null, 2, 1, 100m, false, AccommodationType.HotelRoom);

        // Assert
        room.RoomNumber.Should().BeNull();
        room.Description.Should().BeNull();
    }

    [Fact]
    public void Hide_ShouldSetVisibleToFalse()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);
        room.Visible.Should().BeTrue(); // Verify initial state

        // Act
        room.Hide();

        // Assert
        room.Visible.Should().BeFalse();
    }

    [Fact]
    public void Show_ShouldSetVisibleToTrue()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);
        room.Hide(); // First hide it
        room.Visible.Should().BeFalse();

        // Act
        room.Show();

        // Assert
        room.Visible.Should().BeTrue();
    }

    [Fact]
    public void VisibilityToggle_ShouldWorkCorrectly()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);

        // Assert initial state
        room.Visible.Should().BeTrue();

        // Toggle visibility multiple times
        room.Hide();
        room.Visible.Should().BeFalse();

        room.Show();
        room.Visible.Should().BeTrue();

        room.Hide();
        room.Visible.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 1, 50)]
    [InlineData(4, 2, 150)]
    [InlineData(10, 5, 500)]
    public void Constructor_ShouldAcceptDifferentCapacitiesAndPrices(int capacity, int bedrooms, decimal price)
    {
        // Act
        var room = new Room(1, capacity, bedrooms, price);

        // Assert
        room.Capacity.Should().Be(capacity);
        room.Bedrooms.Should().Be(bedrooms);
        room.PricePerNight.Should().Be(price);
    }

    [Theory]
    [InlineData(AccommodationType.HotelRoom)]
    [InlineData(AccommodationType.Apartment)]
    [InlineData(AccommodationType.House)]
    [InlineData(AccommodationType.Cabin)]
    public void Update_ShouldAcceptAllAccommodationTypes(AccommodationType accommodationType)
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);

        // Act
        room.Update("101", "Test", 2, 1, 100m, false, accommodationType);

        // Assert
        room.Accommodation.Should().Be(accommodationType);
    }

    [Fact]
    public void Update_ShouldNotChangeHotelIdOrCreatedAt()
    {
        // Arrange
        var originalHotelId = 5;
        var room = new Room(originalHotelId, 2, 1, 100m);
        var originalCreatedAt = room.CreatedAt;

        // Act
        room.Update("202", "Updated", 4, 2, 200m, true, AccommodationType.House);

        // Assert - These should remain unchanged
        room.HotelId.Should().Be(originalHotelId);
        room.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Constructor_ShouldSetDecimalPriceCorrectly()
    {
        // Arrange
        var price = 99.99m;

        // Act
        var room = new Room(1, 2, 1, price);

        // Assert
        room.PricePerNight.Should().Be(99.99m);
    }

    [Fact]
    public void Update_ShouldHandleZeroPrice()
    {
        // Arrange
        var room = new Room(1, 2, 1, 100m);

        // Act
        room.Update("101", "Test", 2, 1, 0m, false, AccommodationType.HotelRoom);

        // Assert
        room.PricePerNight.Should().Be(0m);
    }
}
