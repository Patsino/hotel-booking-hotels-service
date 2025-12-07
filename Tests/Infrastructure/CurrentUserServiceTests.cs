using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Tests.Infrastructure;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    private void SetupUser(ClaimsPrincipal? user)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null)
        {
            httpContext.User = user;
        }
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }

    private ClaimsPrincipal CreateAuthenticatedUser(int userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    #region UserId Tests

    [Fact]
    public void UserId_ShouldReturnUserId_WhenUserIsAuthenticated()
    {
        // Arrange
        var expectedUserId = 123;
        var user = CreateAuthenticatedUser(expectedUserId, "test@test.com", "User");
        SetupUser(user);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_ShouldReturnNull_WhenHttpContextIsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _service.UserId;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Email Tests

    [Fact]
    public void Email_ShouldReturnEmail_WhenUserIsAuthenticated()
    {
        // Arrange
        var expectedEmail = "user@example.com";
        var user = CreateAuthenticatedUser(1, expectedEmail, "User");
        SetupUser(user);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void Email_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.Email;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Role Tests

    [Theory]
    [InlineData("Admin")]
    [InlineData("HotelOwner")]
    [InlineData("User")]
    public void Role_ShouldReturnCorrectRole(string expectedRole)
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "test@test.com", expectedRole);
        SetupUser(user);

        // Act
        var result = _service.Role;

        // Assert
        result.Should().Be(expectedRole);
    }

    [Fact]
    public void Role_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.Role;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenUserIsAuthenticated()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "test@test.com", "User");
        SetupUser(user);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenHttpContextIsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _service.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsAdmin Tests

    [Fact]
    public void IsAdmin_ShouldReturnTrue_WhenRoleIsAdmin()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "admin@test.com", "Admin");
        SetupUser(user);

        // Act
        var result = _service.IsAdmin;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_ShouldReturnFalse_WhenRoleIsNotAdmin()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "user@test.com", "User");
        SetupUser(user);

        // Act
        var result = _service.IsAdmin;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsHotelOwner Tests

    [Fact]
    public void IsHotelOwner_ShouldReturnTrue_WhenRoleIsHotelOwner()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "owner@test.com", "HotelOwner");
        SetupUser(user);

        // Act
        var result = _service.IsHotelOwner;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHotelOwner_ShouldReturnFalse_WhenRoleIsNotHotelOwner()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "user@test.com", "User");
        SetupUser(user);

        // Act
        var result = _service.IsHotelOwner;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsUser Tests

    [Fact]
    public void IsUser_ShouldReturnTrue_WhenRoleIsUser()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "user@test.com", "User");
        SetupUser(user);

        // Act
        var result = _service.IsUser;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsUser_ShouldReturnFalse_WhenRoleIsNotUser()
    {
        // Arrange
        var user = CreateAuthenticatedUser(1, "admin@test.com", "Admin");
        SetupUser(user);

        // Act
        var result = _service.IsUser;

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
