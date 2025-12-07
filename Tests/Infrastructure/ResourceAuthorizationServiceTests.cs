using Infrastructure.Authentication;
using Infrastructure.Authorization;

namespace Tests.Infrastructure;

public class ResourceAuthorizationServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly ResourceAuthorizationService _service;

    public ResourceAuthorizationServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _service = new ResourceAuthorizationService(_mockCurrentUserService.Object);
    }

    #region CanAccessResource Tests

    [Fact]
    public void CanAccessResource_ShouldReturnFalse_WhenNotAuthenticated()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

        // Act
        var result = _service.CanAccessResource(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanAccessResource_ShouldReturnTrue_WhenUserIsAdmin()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(999); // Different user

        // Act
        var result = _service.CanAccessResource(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanAccessResource_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var resourceOwnerId = 5;
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(resourceOwnerId);

        // Act
        var result = _service.CanAccessResource(resourceOwnerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanAccessResource_ShouldReturnFalse_WhenUserIsNotOwner()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(10);

        // Act
        var result = _service.CanAccessResource(5); // Different owner

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanModifyResource Tests

    [Fact]
    public void CanModifyResource_ShouldReturnFalse_WhenNotAuthenticated()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

        // Act
        var result = _service.CanModifyResource(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanModifyResource_ShouldReturnTrue_WhenUserIsAdmin()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(true);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(999);

        // Act
        var result = _service.CanModifyResource(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanModifyResource_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var resourceOwnerId = 5;
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(resourceOwnerId);

        // Act
        var result = _service.CanModifyResource(resourceOwnerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanModifyResource_ShouldReturnFalse_WhenUserIsNotOwner()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(10);

        // Act
        var result = _service.CanModifyResource(5);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EnsureCanAccessResource Tests

    [Fact]
    public void EnsureCanAccessResource_ShouldNotThrow_WhenUserCanAccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(true);

        // Act & Assert
        var action = () => _service.EnsureCanAccessResource(1);
        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanAccessResource_ShouldThrow_WhenUserCannotAccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

        // Act & Assert
        var action = () => _service.EnsureCanAccessResource(1);
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to access this resource");
    }

    #endregion

    #region EnsureCanModifyResource Tests

    [Fact]
    public void EnsureCanModifyResource_ShouldNotThrow_WhenUserCanModify()
    {
        // Arrange
        var ownerId = 5;
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(ownerId);

        // Act & Assert
        var action = () => _service.EnsureCanModifyResource(ownerId);
        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanModifyResource_ShouldThrow_WhenUserCannotModify()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(10);

        // Act & Assert
        var action = () => _service.EnsureCanModifyResource(5); // Different owner
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to modify this resource");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CanAccessResource_ShouldReturnFalse_WhenUserIdIsNull()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns((int?)null);

        // Act
        var result = _service.CanAccessResource(1);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void CanModifyResource_ShouldWork_ForDifferentResourceOwnerIds(int ownerId)
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(s => s.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(s => s.UserId).Returns(ownerId);

        // Act
        var result = _service.CanModifyResource(ownerId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
