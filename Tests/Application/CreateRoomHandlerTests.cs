using Application.Commands;
using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;

namespace Tests.Application;

public class CreateRoomHandlerTests
{
    private readonly Mock<IRoomsRepository> _mockRoomsRepository;
    private readonly Mock<IHotelsRepository> _mockHotelsRepository;
    private readonly CreateRoomHandler _handler;

    public CreateRoomHandlerTests()
    {
        _mockRoomsRepository = new Mock<IRoomsRepository>();
        _mockHotelsRepository = new Mock<IHotelsRepository>();
        _handler = new CreateRoomHandler(_mockRoomsRepository.Object, _mockHotelsRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateRoom_WhenHotelExists()
    {
        // Arrange
        var hotelId = 1;
        var hotel = new Hotel(1, "Test Hotel", "USA", "NYC");
        var command = new CreateRoomCommand(
            HotelId: hotelId,
            Capacity: 4,
            Bedrooms: 2,
            PricePerNight: 150m,
            Accommodation: "HotelRoom"
        );

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(hotelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        _mockRoomsRepository.Verify(r => r.AddAsync(
            It.Is<Room>(room =>
                room.HotelId == hotelId &&
                room.Capacity == command.Capacity &&
                room.Bedrooms == command.Bedrooms &&
                room.PricePerNight == command.PricePerNight),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRoomsRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenHotelNotFound()
    {
        // Arrange
        var command = new CreateRoomCommand(
            HotelId: 999, // Non-existent hotel
            Capacity: 2,
            Bedrooms: 1,
            PricePerNight: 100m,
            Accommodation: "HotelRoom"
        );

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        // Act & Assert
        var action = () => _handler.HandleAsync(command);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Hotel not found");
    }

    [Fact]
    public async Task HandleAsync_ShouldCheckHotelExists_BeforeCreatingRoom()
    {
        // Arrange
        var hotelId = 1;
        var hotel = new Hotel(1, "Test", "USA", "NYC");
        var command = new CreateRoomCommand(hotelId, 2, 1, 100m, Accommodation: "HotelRoom");
        var callOrder = new List<string>();

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(hotelId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetByIdAsync"))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        callOrder.Should().ContainInOrder("GetByIdAsync", "AddAsync", "SaveChangesAsync");
    }

    [Theory]
    [InlineData("HotelRoom")]
    [InlineData("Apartment")]
    [InlineData("House")]
    [InlineData("Cabin")]
    public async Task HandleAsync_ShouldParseAccommodationType_Correctly(string accommodation)
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "USA", "NYC");
        var command = new CreateRoomCommand(1, 2, 1, 100m, Accommodation: accommodation);

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert - Should not throw when parsing accommodation type
        _mockRoomsRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetDefaultValues_ForNewRoom()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "USA", "NYC");
        var command = new CreateRoomCommand(1, 2, 1, 100m, Accommodation: "HotelRoom");
        Room? capturedRoom = null;

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback<Room, CancellationToken>((r, _) => capturedRoom = r)
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        capturedRoom.Should().NotBeNull();
        capturedRoom!.Visible.Should().BeTrue();
        capturedRoom.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken_ToAllRepositoryCalls()
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "USA", "NYC");
        var command = new CreateRoomCommand(1, 2, 1, 100m, Accommodation: "HotelRoom");
        var cancellationToken = new CancellationToken();

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command, cancellationToken);

        // Assert
        _mockHotelsRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
        _mockRoomsRepository.Verify(r => r.AddAsync(It.IsAny<Room>(), cancellationToken), Times.Once);
        _mockRoomsRepository.Verify(r => r.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(1, 1, 50)]
    [InlineData(4, 2, 150)]
    [InlineData(10, 5, 500)]
    public async Task HandleAsync_ShouldAcceptDifferentCapacitiesAndPrices(int capacity, int bedrooms, decimal price)
    {
        // Arrange
        var hotel = new Hotel(1, "Test", "USA", "NYC");
        var command = new CreateRoomCommand(1, capacity, bedrooms, price, Accommodation: "HotelRoom");
        Room? capturedRoom = null;

        _mockHotelsRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _mockRoomsRepository
            .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback<Room, CancellationToken>((r, _) => capturedRoom = r)
            .Returns(Task.CompletedTask);

        _mockRoomsRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        capturedRoom.Should().NotBeNull();
        capturedRoom!.Capacity.Should().Be(capacity);
        capturedRoom.Bedrooms.Should().Be(bedrooms);
        capturedRoom.PricePerNight.Should().Be(price);
    }
}
