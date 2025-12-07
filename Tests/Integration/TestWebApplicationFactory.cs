using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<HotelsDbContext>));

            // Add InMemory database with unique name
            services.AddDbContext<HotelsDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
            });

            // Replace authentication with test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Configure default authentication scheme
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(int? userId, string? role)
    {
        var client = CreateClient();
        
        if (userId.HasValue && !string.IsNullOrEmpty(role))
        {
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId.Value.ToString());
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }
        else
        {
            client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        }
        
        return client;
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for anonymous header
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Read user info from request headers
        var userIdStr = Request.Headers["X-Test-UserId"].FirstOrDefault();
        var role = Request.Headers["X-Test-Role"].FirstOrDefault();

        if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role))
        {
            // Default to authenticated HotelOwner with userId=1
            userIdStr = "1";
            role = "HotelOwner";
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userIdStr),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
