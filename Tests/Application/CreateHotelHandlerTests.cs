using Application.Commands;
using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;

namespace Tests.Application;

public class CreateHotelHandlerTests
{
    private readonly Mock<IHotelsRepository> _mockRepository;
    private readonly CreateHotelHandler _handler;

    public CreateHotelHandlerTests()
    {
        _mockRepository = new Mock<IHotelsRepository>();
        _handler = new CreateHotelHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateHotel_AndReturnId()
    {
        // Arrange
        var command = new CreateHotelCommand(
            OwnerId: 1,
            Name: "Test Hotel",
            Country: "USA",
            City: "New York"
        );

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<Hotel>(h => 
                h.OwnerId == command.OwnerId &&
                h.Name == command.Name &&
                h.Country == command.Country &&
                h.City == command.City),
            It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepository_InCorrectOrder()
    {
        // Arrange
        var command = new CreateHotelCommand(1, "Test", "USA", "NYC");
        var callOrder = new List<string>();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        callOrder.Should().ContainInOrder("AddAsync", "SaveChangesAsync");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetDefaultApprovalStatus_ToPending()
    {
        // Arrange
        var command = new CreateHotelCommand(1, "Test", "USA", "NYC");
        Hotel? capturedHotel = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()))
            .Callback<Hotel, CancellationToken>((h, _) => capturedHotel = h)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        capturedHotel.Should().NotBeNull();
        capturedHotel!.Approval.Should().Be(ApprovalStatus.Pending);
    }

    [Theory]
    [InlineData(1, "Hotel A", "USA", "New York")]
    [InlineData(2, "Hotel B", "UK", "London")]
    [InlineData(3, "Hotel C", "France", "Paris")]
    public async Task HandleAsync_ShouldAcceptDifferentCommands(int ownerId, string name, string country, string city)
    {
        // Arrange
        var command = new CreateHotelCommand(ownerId, name, country, city);
        Hotel? capturedHotel = null;

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()))
            .Callback<Hotel, CancellationToken>((h, _) => capturedHotel = h)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        capturedHotel.Should().NotBeNull();
        capturedHotel!.OwnerId.Should().Be(ownerId);
        capturedHotel.Name.Should().Be(name);
        capturedHotel.Country.Should().Be(country);
        capturedHotel.City.Should().Be(city);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken_ToRepository()
    {
        // Arrange
        var command = new CreateHotelCommand(1, "Test", "USA", "NYC");
        var cancellationToken = new CancellationToken();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Hotel>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Hotel>(), cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(cancellationToken), Times.Once);
    }
}
